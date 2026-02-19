import React from 'react';
import { Outlet } from 'react-router-dom';
import { Box } from '@mui/material';

const AppLayout: React.FC = () => {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: '100vh',
        width: '100%',
      }}
    >
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: '100%',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Box sx={{ flexGrow: 1, width: '100%', height: '100%' }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
};

export default AppLayout;
