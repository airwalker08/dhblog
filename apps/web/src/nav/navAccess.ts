import type { FeatureAccess } from '../types';
import { buildNavTree, findNavNode } from './buildNavTree';

/** True when the nav tree includes this group (parent or synthesized from children). */
export function canAccessNavGroup(features: FeatureAccess[], groupCode: string): boolean {
  return findNavNode(buildNavTree(features), groupCode) !== undefined;
}
