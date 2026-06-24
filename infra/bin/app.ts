#!/usr/bin/env node
import * as cdk from 'aws-cdk-lib';
import { getEnvConfig } from '../config/env';
import { NetworkStack } from '../lib/network-stack';
import { DataStack } from '../lib/data-stack';
import { MediaStack } from '../lib/media-stack';
import { EcrStack } from '../lib/ecr-stack';
import { ConfigStack } from '../lib/config-stack';
import { AppStack } from '../lib/app-stack';
import { PipelineStack } from '../lib/pipeline-stack';

const app = new cdk.App();
const envName = app.node.tryGetContext('env') ?? 'dev';
const config = getEnvConfig(envName);

const stackEnv = {
  account: config.account,
  region: config.region,
};

const network = new NetworkStack(app, `DhblogNetwork-${envName}`, { env: stackEnv });
const data = new DataStack(app, `DhblogData-${envName}`, config, { env: stackEnv });
const media = new MediaStack(app, `DhblogMedia-${envName}`, config, { env: stackEnv });
const ecr = new EcrStack(app, `DhblogEcr-${envName}`, config, { env: stackEnv });
const configStack = new ConfigStack(app, `DhblogConfig-${envName}`, config, { env: stackEnv });

new AppStack(app, `DhblogApp-${envName}`, {
  env: stackEnv,
  vpc: network.vpc,
  apiRepo: ecr.apiRepo,
  webRepo: ecr.webRepo,
  tables: data.tables,
  mediaBucket: media.bucket,
  jwtSecretParam: configStack.jwtSecretParam,
  config,
});

const connectionArn = app.node.tryGetContext('githubConnectionArn');
const githubOwner = app.node.tryGetContext('githubOwner') ?? 'your-org';
const githubRepo = app.node.tryGetContext('githubRepo') ?? 'dhblog';

if (connectionArn) {
  new PipelineStack(app, `DhblogPipeline-${envName}`, {
    env: stackEnv,
    config,
    githubOwner,
    githubRepo,
    githubBranch: envName === 'prod' ? 'main' : 'develop',
    connectionArn,
  });
}

app.synth();
