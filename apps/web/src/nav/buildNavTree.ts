import type { FeatureAccess } from '../types';

export interface NavNode extends FeatureAccess {
  children: NavNode[];
}

function toNode(feature: FeatureAccess): NavNode {
  return {
    ...feature,
    parentCode: feature.parentCode ?? null,
    children: [],
  };
}

function sortTree(nodes: NavNode[]): void {
  nodes.sort((a, b) => a.sortOrder - b.sortOrder);
  for (const node of nodes) {
    if (node.children.length > 0) sortTree(node.children);
  }
}

export function buildNavTree(features: FeatureAccess[]): NavNode[] {
  const accessible = features.filter((f) => f.canRead && f.navPath);
  const nodes = new Map(accessible.map((f) => [f.code, toNode(f)]));
  const roots: NavNode[] = [];

  for (const feature of accessible) {
    const node = nodes.get(feature.code)!;
    const parentCode = feature.parentCode?.trim() || null;
    const parent = parentCode ? nodes.get(parentCode) : undefined;

    if (parent) {
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  }

  sortTree(roots);
  return roots;
}

export function findNavNode(nodes: NavNode[], code: string): NavNode | undefined {
  for (const node of nodes) {
    if (node.code === code) return node;
    const child = findNavNode(node.children, code);
    if (child) return child;
  }
  return undefined;
}
