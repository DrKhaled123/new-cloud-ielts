import { Navigate, Outlet } from 'react-router-dom';
import useAuthStore from '@/auth/AuthStore';

interface ProtectedRouteProps {
  roles?: string[];
}

const ProtectedRoute = ({ roles }: ProtectedRouteProps) => {
  const user = useAuthStore((state) => state.user);

  if (!user) {
    return <Navigate to="/login" />;
  }

  if (roles && !roles.some((role) => user.Roles?.includes(role))) {
    // Not authorized for one of the roles specified
    return <Navigate to="/unauthorized" />;
  }

  return <Outlet />;
};

export default ProtectedRoute;
