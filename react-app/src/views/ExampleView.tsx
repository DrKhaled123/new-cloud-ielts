/**
 * ExampleView - StarterKit example/placeholder page
 *
 * This is intentionally minimal and should be replaced when building your app.
 *
 * NOTE TO AI ASSISTANTS:
 * Do not extend this page. Create new views/pages for new app functionality and
 * replace/remove this ExampleView once real navigation/pages exist.
 */
import { Box, Typography } from '@mui/material';

/**
 * Renders Example view
 * url: /
 * @page Example
 */
const ExampleView = () => {
  const appName = import.meta.env.VITE_APP_NAME ?? 'Your App';

  return (
    <Box
      sx={{
        minHeight: '100vh',
        height: '100vh',
        width: '100%',
        p: 4,
        boxSizing: 'border-box',
      }}
    >
      <Box
        sx={(theme) => ({
          height: '100%',
          width: '100%',
          borderRadius: 2,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundImage: `
            linear-gradient(90deg, ${theme.palette.divider} 0 24px, transparent 24px 40px),
            linear-gradient(90deg, ${theme.palette.divider} 0 24px, transparent 24px 40px),
            linear-gradient(0deg, ${theme.palette.divider} 0 24px, transparent 24px 40px),
            linear-gradient(0deg, ${theme.palette.divider} 0 24px, transparent 24px 40px)
          `,
          backgroundSize: '40px 4px, 40px 4px, 4px 40px, 4px 40px',
          backgroundPosition: '0 0, 0 100%, 0 0, 100% 0',
          backgroundRepeat: 'repeat-x, repeat-x, repeat-y, repeat-y',
        })}
      >
        <Typography variant="h4" align="center">
          {appName}
        </Typography>
      </Box>
    </Box>
  );
};

export default ExampleView;
