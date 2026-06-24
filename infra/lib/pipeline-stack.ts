import * as cdk from 'aws-cdk-lib';
import * as codebuild from 'aws-cdk-lib/aws-codebuild';
import * as codepipeline from 'aws-cdk-lib/aws-codepipeline';
import * as codepipeline_actions from 'aws-cdk-lib/aws-codepipeline-actions';
import * as iam from 'aws-cdk-lib/aws-iam';
import { Construct } from 'constructs';
import { EnvConfig } from '../config/env';

export interface PipelineStackProps extends cdk.StackProps {
  config: EnvConfig;
  githubOwner: string;
  githubRepo: string;
  githubBranch: string;
  connectionArn: string;
}

export class PipelineStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props: PipelineStackProps) {
    super(scope, id, props);

    const sourceOutput = new codepipeline.Artifact();
    const buildOutput = new codepipeline.Artifact();

    const buildProject = new codebuild.PipelineProject(this, 'BuildProject', {
      projectName: `dhblog-build-${props.config.envName}`,
      environment: {
        buildImage: codebuild.LinuxBuildImage.STANDARD_7_0,
        privileged: true,
      },
      environmentVariables: {
        DHBLOG_ENV: { value: props.config.envName },
        AWS_REGION: { value: props.config.region },
        AWS_ACCOUNT_ID: { value: cdk.Aws.ACCOUNT_ID },
      },
      buildSpec: codebuild.BuildSpec.fromSourceFilename('buildspec.yml'),
    });

    buildProject.addToRolePolicy(new iam.PolicyStatement({
      actions: [
        'ecr:GetAuthorizationToken', 'ecr:BatchCheckLayerAvailability', 'ecr:GetDownloadUrlForLayer',
        'ecr:BatchGetImage', 'ecr:PutImage', 'ecr:InitiateLayerUpload', 'ecr:UploadLayerPart', 'ecr:CompleteLayerUpload',
        'cloudformation:*', 'iam:*', 'ecs:*', 'ec2:*', 'elasticloadbalancing:*',
        'dynamodb:*', 's3:*', 'ssm:*', 'logs:*', 'cloudfront:*',
      ],
      resources: ['*'],
    }));

    const pipeline = new codepipeline.Pipeline(this, 'Pipeline', {
      pipelineName: `dhblog-${props.config.envName}`,
    });

    pipeline.addStage({
      stageName: 'Source',
      actions: [
        new codepipeline_actions.CodeStarConnectionsSourceAction({
          actionName: 'GitHub',
          owner: props.githubOwner,
          repo: props.githubRepo,
          branch: props.githubBranch,
          connectionArn: props.connectionArn,
          output: sourceOutput,
        }),
      ],
    });

    pipeline.addStage({
      stageName: 'Build',
      actions: [
        new codepipeline_actions.CodeBuildAction({
          actionName: 'BuildAndDeploy',
          project: buildProject,
          input: sourceOutput,
          outputs: [buildOutput],
        }),
      ],
    });
  }
}
