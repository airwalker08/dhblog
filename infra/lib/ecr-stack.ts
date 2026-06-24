import * as cdk from 'aws-cdk-lib';
import * as ecr from 'aws-cdk-lib/aws-ecr';
import { Construct } from 'constructs';
import { EnvConfig } from '../config/env';

export class EcrStack extends cdk.Stack {
  public readonly apiRepo: ecr.Repository;
  public readonly webRepo: ecr.Repository;

  constructor(scope: Construct, id: string, config: EnvConfig, props?: cdk.StackProps) {
    super(scope, id, props);

    this.apiRepo = new ecr.Repository(this, 'ApiRepo', {
      repositoryName: `dhblog-api-${config.envName}`,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
      emptyOnDelete: true,
    });

    this.webRepo = new ecr.Repository(this, 'WebRepo', {
      repositoryName: `dhblog-web-${config.envName}`,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
      emptyOnDelete: true,
    });
  }
}
