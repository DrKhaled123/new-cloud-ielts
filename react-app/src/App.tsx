import ErrorBoundary from '@/components/ErrorBoundary';
import Routes from '@/routes/Routes';
import ThemeProvider from '@/theme/ThemeProvider';
import { AuthProvider } from './auth/AuthProvider';

/**
 * Root Application Component
 * @component MainApp
 */
const MainApp = () => {
  return (
    <ErrorBoundary name="App">
      <ThemeProvider>
        <AuthProvider>
          <Routes />
        </AuthProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
};

export default MainApp;
