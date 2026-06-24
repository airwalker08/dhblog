import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { RichTreeView } from '@mui/x-tree-view/RichTreeView';
import type { NavNode } from '../nav/buildNavTree';
import { findNavNode, findSelectedItemId, getExpandedIds } from '../nav/treeViewNav';

function TreeExpandIcon(props: React.HTMLAttributes<HTMLSpanElement>) {
  const { className, ...rest } = props;
  return (
    <span
      {...rest}
      className={['material-icons', 'dhblog-side-nav__chevron', className].filter(Boolean).join(' ')}
      aria-hidden
    >
      chevron_right
    </span>
  );
}

function TreeCollapseIcon(props: React.HTMLAttributes<HTMLSpanElement>) {
  const { className, ...rest } = props;
  return (
    <span
      {...rest}
      className={['material-icons', 'dhblog-side-nav__chevron', className].filter(Boolean).join(' ')}
      aria-hidden
    >
      expand_more
    </span>
  );
}

export function SidebarNav({ nodes }: { nodes: NavNode[] }) {
  const { pathname } = useLocation();
  const navigate = useNavigate();

  const selectedItem = useMemo(() => findSelectedItemId(nodes, pathname), [nodes, pathname]);
  const [expandedItems, setExpandedItems] = useState<string[]>(() => getExpandedIds(nodes, pathname));

  useEffect(() => {
    const required = getExpandedIds(nodes, pathname);
    if (required.length === 0) return;
    setExpandedItems((prev) => [...new Set([...prev, ...required])]);
  }, [nodes, pathname]);

  const handleItemClick = (_event: React.MouseEvent, itemId: string) => {
    const navPath = findNavNode(nodes, itemId)?.navPath;
    if (navPath) navigate(navPath);
  };

  return (
    <RichTreeView
      className="dhblog-side-nav"
      aria-label="Main navigation"
      items={nodes}
      getItemId={(item) => item.code}
      getItemLabel={(item) => item.name}
      getItemChildren={(item) => (item.children.length > 0 ? item.children : undefined)}
      expandedItems={expandedItems}
      onExpandedItemsChange={(_event, itemIds) => setExpandedItems(itemIds)}
      expansionTrigger="iconContainer"
      selectedItems={selectedItem}
      onItemClick={handleItemClick}
      itemHeight={40}
      slots={{
        expandIcon: TreeExpandIcon,
        collapseIcon: TreeCollapseIcon,
        endIcon: null,
      }}
    />
  );
}
