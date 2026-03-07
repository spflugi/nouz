import { useEffect } from 'react';
import { MainLayout } from './components/layout/MainLayout';
import { useSettingsStore } from './store/settingsStore';

function App() {
  const { load } = useSettingsStore();

  useEffect(() => {
    load();
  }, []);

  return <MainLayout />;
}

export default App;
