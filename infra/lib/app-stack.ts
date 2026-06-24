import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as ecr from 'aws-cdk-lib/aws-ecr';
import * as elbv2 from 'aws-cdk-lib/aws-elasticloadbalancingv2';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as logs from 'aws-cdk-lib/aws-logs';
import * as s3 from 'aws-cdk-lib/aws-s3';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import { Construct } from 'constructs';
import { EnvConfig } from '../config/env';

export interface AppStackProps extends cdk.StackProps {
  vpc: ec2.IVpc;
  apiRepo: ecr.IRepository;
  webRepo: ecr.IRepository;
  tables: dynamodb.ITable[];
  mediaBucket: s3.IBucket;
  jwtSecretParam: ssm.IStringParameter;
  config: EnvConfig;
}

export class AppStack extends cdk.Stack {
  public readonly cluster: ecs.Cluster;
  public readonly apiService: ecs.FargateService;
  public readonly webService: ecs.FargateService;
  public readonly loadBalancer: elbv2.ApplicationLoadBalancer;

  constructor(scope: Construct, id: string, props: AppStackProps) {
    super(scope, id, props);

    this.cluster = new ecs.Cluster(this, 'Cluster', {
      vpc: props.vpc,
      clusterName: `dhblog-${props.config.envName}`,
    });

    const taskRole = new iam.Role(this, 'TaskRole', {
      assumedBy: new iam.ServicePrincipal('ecs-tasks.amazonaws.com'),
    });

    for (const table of props.tables) {
      table.grantReadWriteData(taskRole);
    }
    props.mediaBucket.grantReadWrite(taskRole);
    taskRole.addToPolicy(new iam.PolicyStatement({
      actions: ['ssm:GetParameter*', 'ssm:PutParameter', 'ssm:GetParametersByPath'],
      resources: [`arn:aws:ssm:${props.config.region}:${cdk.Aws.ACCOUNT_ID}:parameter/dhblog/${props.config.envName}/*`],
    }));
    taskRole.addToPolicy(new iam.PolicyStatement({
      actions: ['sts:GetCallerIdentity'],
      resources: ['*'],
    }));

    const apiTaskDef = new ecs.FargateTaskDefinition(this, 'ApiTaskDef', {
      memoryLimitMiB: 512,
      cpu: 256,
      taskRole,
    });

    apiTaskDef.addContainer('Api', {
      image: ecs.ContainerImage.fromEcrRepository(props.apiRepo, 'latest'),
      logging: ecs.LogDrivers.awsLogs({ streamPrefix: 'api', logRetention: logs.RetentionDays.ONE_WEEK }),
      environment: {
        DHBLOG_ENV: props.config.envName,
        AWS_REGION: props.config.region,
        ASPNETCORE_ENVIRONMENT: 'Production',
        MEDIA_BUCKET_NAME: props.mediaBucket.bucketName,
      },
      secrets: {
        JWT_SECRET: ecs.Secret.fromSsmParameter(props.jwtSecretParam),
      },
      portMappings: [{ containerPort: 8080 }],
    });

    const webTaskDef = new ecs.FargateTaskDefinition(this, 'WebTaskDef', {
      memoryLimitMiB: 256,
      cpu: 256,
    });

    webTaskDef.addContainer('Web', {
      image: ecs.ContainerImage.fromEcrRepository(props.webRepo, 'latest'),
      logging: ecs.LogDrivers.awsLogs({ streamPrefix: 'web', logRetention: logs.RetentionDays.ONE_WEEK }),
      portMappings: [{ containerPort: 80 }],
    });

    this.loadBalancer = new elbv2.ApplicationLoadBalancer(this, 'Alb', {
      vpc: props.vpc,
      internetFacing: true,
    });

    this.apiService = new ecs.FargateService(this, 'ApiService', {
      cluster: this.cluster,
      taskDefinition: apiTaskDef,
      desiredCount: 1,
      assignPublicIp: false,
      vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
    });

    this.webService = new ecs.FargateService(this, 'WebService', {
      cluster: this.cluster,
      taskDefinition: webTaskDef,
      desiredCount: 1,
      assignPublicIp: false,
      vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
    });

    const apiTg = new elbv2.ApplicationTargetGroup(this, 'ApiTg', {
      vpc: props.vpc,
      port: 8080,
      protocol: elbv2.ApplicationProtocol.HTTP,
      targets: [this.apiService],
      healthCheck: { path: '/api/health' },
    });

    const webTg = new elbv2.ApplicationTargetGroup(this, 'WebTg', {
      vpc: props.vpc,
      port: 80,
      protocol: elbv2.ApplicationProtocol.HTTP,
      targets: [this.webService],
      healthCheck: { path: '/' },
    });

    const listener = this.loadBalancer.addListener('Http', {
      port: 80,
      defaultTargetGroups: [webTg],
    });

    listener.addAction('ApiRoute', {
      priority: 10,
      conditions: [elbv2.ListenerCondition.pathPatterns(['/api/*'])],
      action: elbv2.ListenerAction.forward([apiTg]),
    });

    new cdk.CfnOutput(this, 'LoadBalancerDns', { value: this.loadBalancer.loadBalancerDnsName });
  }
}
