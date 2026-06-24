import type { FeatureAccess } from '../types';
import { NAV_GROUPS, type NavGroupDefinition } from './navHierarchy';

export interface NavNode extends FeatureAccess {
  children: NavNode[];
  isGroupParent: boolean;
}

function toNode(feature: FeatureAccess, isGroupParent = false): NavNode {
  return {
    ...feature,
    parentCode: feature.parentCode ?? null,
    children: [],
    isGroupParent,
  };
}

function synthesizeParent(group: NavGroupDefinition): FeatureAccess {
  return {
    code: group.code,
    name: group.name,
    navPath: group.navPath,
    sortOrder: group.sortOrder,
    canRead: true,
    canWrite: false,
    parentCode: null,
  };
}

export function buildNavTree(features: FeatureAccess[]): NavNode[] {
  const accessible = features.filter((f) => f.canRead && f.navPath);
  const byCode = new Map(accessible.map((f) => [f.code, f]));
  const used = new Set<string>();
  const roots: NavNode[] = [];

  for (const group of NAV_GROUPS) {
    const parentFeature = byCode.get(group.code);
    const childFeatures = group.childCodes
      .map((code) => byCode.get(code))
      .filter((f): f is FeatureAccess => f !== undefined);

    if (!parentFeature && childFeatures.length === 0) continue;

    const parentSource = parentFeature ?? synthesizeParent(group);
    const parentNode = toNode(parentSource, true);
    parentNode.children = childFeatures
      .map((child) => toNode(child))
      .sort((a, b) => a.sortOrder - b.sortOrder);

    roots.push(parentNode);
    used.add(group.code);
    childFeatures.forEach((child) => used.add(child.code));
  }

  for (const feature of accessible) {
    if (used.has(feature.code)) continue;
    roots.push(toNode(feature));
  }

  return roots.sort((a, b) => a.sortOrder - b.sortOrder);
}

export function findNavNode(nodes: NavNode[], code: string): NavNode | undefined {
  for (const node of nodes) {
    if (node.code === code) return node;
    const child = findNavNode(node.children, code);
    if (child) return child;
  }
  return undefined;
}
