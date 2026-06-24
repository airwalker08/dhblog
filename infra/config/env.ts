export interface EnvConfig {
  envName: string;
  account?: string;
  region: string;
}

export function getEnvConfig(envName: string): EnvConfig {
  return {
    envName,
    account: process.env.CDK_DEFAULT_ACCOUNT,
    region: process.env.CDK_DEFAULT_REGION ?? 'us-east-1',
  };
}

export const TABLE_BASE_NAMES = [
  'dhblog-users',
  'dhblog-roles',
  'dhblog-features',
  'dhblog-feature-roles',
  'dhblog-blog-entries',
  'dhblog-blog-images',
  'dhblog-topics',
  'dhblog-blog-entry-topics',
  'dhblog-user-follows',
  'dhblog-password-reset-tokens',
] as const;

export const DEFAULT_SETTINGS: Record<string, string> = {
  blog_entry_text_len: '250',
  blog_entry_max_img_count: '10',
  blog_entry_max_img_types: 'png,jpg',
  blog_entry_max_img_size: '1048576',
  jwt_expiry_minutes: '60',
  site_name: 'dhblog',
  password_reset_token_ttl_minutes: '30',
};
