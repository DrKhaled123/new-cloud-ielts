// Background.tsx
import React from 'react';
import Box, { BoxProps } from '@mui/material/Box';
import { useTheme } from '@mui/material/styles';

interface BackgroundProps extends BoxProps {
  // Add any extra props if needed.
}

const CanvasBackground: React.FC<BackgroundProps> = ({ children, sx, ...otherProps }) => {
  const theme = useTheme(); // Access Material-UI theme for consistent styling

  return (
    <Box
      sx={{
        position: 'relative', // Needed for the pseudo-element to position correctly.
        width: '100%',
        height: '100%',
        minHeight: '100%',
        backgroundColor: theme.palette.background.default, // Use Material-UI's default background color
        overflowY: 'auto',
        boxSizing: 'border-box',
        padding: 2,
        display: 'flex',
        flexWrap: 'wrap',
        gap: 2,
        // Add a pseudo-element to render the background image with 40% opacity.
        '&::before': {
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          backgroundImage: 'url(img/texture.png)',
          backgroundSize: '400px 400px',
          backgroundRepeat: 'repeat',
          opacity: 0.4, // Set the image opacity to 40%
          zIndex: 0,
        },
        // Ensure that children are rendered above the pseudo-element.
        '& > *': {
          position: 'relative',
          zIndex: 1,
        },
        ...sx, // Allow style overrides
      }}
      {...otherProps}
    >
      {children}
    </Box>
  );
};

export default CanvasBackground;
