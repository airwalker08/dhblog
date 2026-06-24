import { Navigate } from 'react-router-dom';
import { useAuth } from './AuthContext';

export function FeatureRoute({
  code,
  write = false,
  children,
}: {
  code: string;
  write?: boolean;
  children: React.ReactNode;
}) {
  const { hasFeature } = useAuth();
  if (!hasFeature(code, write)) {
    return <Navigate to="/feed" replace />;
  }
  return children;
}
