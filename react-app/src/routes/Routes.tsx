import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import AppLayout from '@/theme/AppLayout';
import NotFoundView from '@/views/NotFoundView';
import ExampleView from '@/views/ExampleView';
import ProtectedRoute from '@/routes/ProtectedRoute';

/**
 * Define the single set of routes with public and protected routes
 * 
 * To restrict a route by role (such as ADMIN), wrap it like so:
 *
 *   <Route element={<ProtectedRoute roles={[ADMIN_ROLE]} />}>
 *     <Route path="some-path" element={<SomeComponent />} />
 *   </Route>
 *
 * Where ADMIN_ROLE is imported from @/utils/roles and can be any role your app defines.
 */
const App = () => {
  return (
    <Router>
      <Routes>
        {/* Public Routes */}
        <Route index element={<ExampleView />} />
        {/* Fallback to NotFound officially handled below */}

        {/* All protected routes */}
        <Route element={<ProtectedRoute />}>

          {/* Main AppLayout-protected tree */}
          <Route path="/" element={<AppLayout />}>
            {/**
              Example of restricting a route by role:
              <Route element={<ProtectedRoute roles={[ADMIN_ROLE]} />}>
                <Route path="admin-stuff" element={<AdminStuffView />} />
              </Route>
            */}
          </Route>
        </Route>

        {/* Fallback routes */}
        <Route path="*" element={<NotFoundView />} />
      </Routes>
    </Router>
  );
};

export default App;
