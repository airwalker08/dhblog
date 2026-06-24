/**
 * Declarative parent/child nav groups. Feature codes must match seeded features.
 * Add new groups here to reuse the tree layout for other feature families.
 */
export interface NavGroupDefinition {
  code: string;
  name: string;
  navPath: string;
  sortOrder: number;
  childCodes: readonly string[];
}

export const NAV_GROUPS: readonly NavGroupDefinition[] = [
  {
    code: 'ADMIN',
    name: 'Admin',
    navPath: '/admin',
    sortOrder: 30,
    childCodes: [
      'ADMIN_USERS',
      'ADMIN_ROLES',
      'ADMIN_TOPICS',
      'SETTINGS',
      'DIAGNOSTICS',
    ],
  },
];

export function getNavGroupForChild(code: string): NavGroupDefinition | undefined {
  return NAV_GROUPS.find((g) => g.childCodes.includes(code));
}
