export interface FeatureAccess {
  code: string;
  name: string;
  navPath: string;
  sortOrder: number;
  canRead: boolean;
  canWrite: boolean;
  parentCode?: string | null;
}

export interface UserProfile {
  userId: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  locale: string;
  timeZone: string;
  language: string;
  roleName: string;
  features: FeatureAccess[];
}

export interface LoginResponse {
  token: string;
  user: UserProfile;
}

export interface BlogEntry {
  entryId: string;
  userId: string;
  username: string;
  title: string;
  text: string;
  createdAt: string;
  updatedAt: string;
  topics: string[];
  images: { imageId: string; url: string; contentType: string }[];
}

export interface PagedBlogEntries {
  items: BlogEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TopicSuggestion {
  topicId: string;
  displayText: string;
  normalizedKey: string;
}

export interface AdminUser {
  userId: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  roleId: string;
  roleName: string;
  locale: string;
  timeZone: string;
  language: string;
}

export interface AdminRole {
  roleId: string;
  name: string;
  description: string;
}

export interface AdminTopic {
  topicId: string;
  displayText: string;
  normalizedKey: string;
  createdByUserId: string;
  createdAt: string;
}
