import { FunctionComponent, useMemo, PropsWithChildren, useState, useEffect, createContext, useContext } from 'react';
import { ThemeProvider as EmotionThemeProvider } from '@emotion/react';
import { CssBaseline } from '@mui/material';
import { createTheme, ThemeOptions } from '@mui/material/styles';
import themeOptions from './Theme.json';

type ThemeOptionsWithModes = ThemeOptions & {
  paletteModes?: Record<'light' | 'dark', ThemeOptions['palette']>;
};

const ThemeContext = createContext({ toggleDarkMode: () => {}, darkMode: false });
const { paletteModes, ...baseThemeOptions } = themeOptions as ThemeOptionsWithModes;

const AppThemeProvider: FunctionComponent<PropsWithChildren> = ({ children }) => {
  const [darkMode, setDarkMode] = useState<boolean>(false);
  const [loading, setLoading] = useState(true);

  const toggleDarkMode = () => {
    setDarkMode((prevDarkMode) => !prevDarkMode);
  };

  useEffect(() => {
    setLoading(false);
  }, []);

  const currentTheme = useMemo(
    () => {
      const palette = paletteModes?.[darkMode ? 'dark' : 'light'] ?? baseThemeOptions.palette;

      return createTheme({
        ...baseThemeOptions,
        palette,
      });
    },
    [darkMode],
  );

  if (loading) return null;

  return (
    <ThemeContext.Provider value={{ toggleDarkMode, darkMode }}>
      <EmotionThemeProvider theme={currentTheme}>
        <CssBaseline enableColorScheme />
        {children}
      </EmotionThemeProvider>
    </ThemeContext.Provider>
  );
};

export const useThemeContext = () => useContext(ThemeContext);

export default AppThemeProvider;
