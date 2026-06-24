import * as cdk from 'aws-cdk-lib';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import { Construct } from 'constructs';
import { EnvConfig, TABLE_BASE_NAMES } from '../config/env';

export class DataStack extends cdk.Stack {
  public readonly tables: dynamodb.Table[] = [];

  constructor(scope: Construct, id: string, config: EnvConfig, props?: cdk.StackProps) {
    super(scope, id, props);

    const tableDefs: Record<string, { pk: string; sk?: string; gsis?: { name: string; pk: string; sk?: string }[] }> = {
      'dhblog-users': {
        pk: 'UserId',
        gsis: [
          { name: 'UsernameIndex', pk: 'Username' },
          { name: 'EmailIndex', pk: 'Email' },
        ],
      },
      'dhblog-roles': { pk: 'RoleId' },
      'dhblog-features': { pk: 'FeatureId', gsis: [{ name: 'FeatureCodeIndex', pk: 'Code' }] },
      'dhblog-feature-roles': { pk: 'RoleId', sk: 'FeatureId' },
      'dhblog-blog-entries': { pk: 'EntryId', gsis: [{ name: 'UserIdCreatedAtIndex', pk: 'UserId', sk: 'CreatedAt' }] },
      'dhblog-blog-images': { pk: 'ImageId', gsis: [{ name: 'EntryIdIndex', pk: 'EntryId' }] },
      'dhblog-topics': { pk: 'TopicId', gsis: [{ name: 'NormalizedKeyIndex', pk: 'NormalizedKey' }] },
      'dhblog-blog-entry-topics': { pk: 'EntryId', sk: 'TopicId', gsis: [{ name: 'TopicIdIndex', pk: 'TopicId', sk: 'EntryId' }] },
      'dhblog-user-follows': { pk: 'FollowerId', sk: 'FollowingId', gsis: [{ name: 'FollowingIdIndex', pk: 'FollowingId', sk: 'FollowerId' }] },
      'dhblog-password-reset-tokens': { pk: 'TokenId', gsis: [{ name: 'TokenIndex', pk: 'Token' }] },
    };

    for (const baseName of TABLE_BASE_NAMES) {
      const def = tableDefs[baseName];
      const tableName = `${baseName}-${config.envName}`;
      const table = new dynamodb.Table(this, tableName, {
        tableName,
        partitionKey: { name: def.pk, type: dynamodb.AttributeType.STRING },
        sortKey: def.sk ? { name: def.sk, type: dynamodb.AttributeType.STRING } : undefined,
        billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
        removalPolicy: config.envName === 'prod' ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
      });

      for (const gsi of def.gsis ?? []) {
        table.addGlobalSecondaryIndex({
          indexName: gsi.name,
          partitionKey: { name: gsi.pk, type: dynamodb.AttributeType.STRING },
          sortKey: gsi.sk ? { name: gsi.sk, type: dynamodb.AttributeType.STRING } : undefined,
          projectionType: dynamodb.ProjectionType.ALL,
        });
      }

      this.tables.push(table);
    }
  }
}
