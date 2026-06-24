import type { NavNode } from './buildNavTree';

export function findNavNode(nodes: NavNode[], code: string): NavNode | undefined {
  for (const node of nodes) {
    if (node.code === code) return node;
    const child = findNavNode(node.children, code);
    if (child) return child;
  }
  return undefined;
}

export function findSelectedItemId(nodes: NavNode[], pathname: string): string | null {
  for (const node of nodes) {
    if (node.navPath && pathname === node.navPath) return node.code;
    const childId = findSelectedItemId(node.children, pathname);
    if (childId) return childId;
  }
  return null;
}

export function getExpandedIds(nodes: NavNode[], pathname: string): string[] {
  const expanded: string[] = [];
  for (const node of nodes) {
    if (node.children.length === 0) continue;
    const childActive = node.children.some(
      (c) => c.navPath && (pathname === c.navPath || pathname.startsWith(`${c.navPath}/`)),
    );
    const selfActive = node.navPath && pathname === node.navPath;
    if (childActive || selfActive) expanded.push(node.code);
  }
  return expanded;
}
