import * as cdk from 'aws-cdk-lib';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import { Construct } from 'constructs';
import { DEFAULT_SETTINGS, EnvConfig } from '../config/env';

export class ConfigStack extends cdk.Stack {
  public readonly jwtSecretParam: ssm.StringParameter;

  constructor(scope: Construct, id: string, config: EnvConfig, props?: cdk.StackProps) {
    super(scope, id, props);

    for (const [key, value] of Object.entries(DEFAULT_SETTINGS)) {
      new ssm.StringParameter(this, `Setting-${key}`, {
        parameterName: `/dhblog/${config.envName}/settings/${key}`,
        stringValue: value,
      });
    }

    this.jwtSecretParam = new ssm.StringParameter(this, 'JwtSecret', {
      parameterName: `/dhblog/${config.envName}/secrets/jwt`,
      stringValue: 'change-me-after-deploy-use-random-secret',
      type: ssm.ParameterType.SECURE_STRING,
    });
  }
}
