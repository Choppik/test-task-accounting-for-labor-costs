import React from 'react';
import { Card } from '@blueprintjs/core';
import Home from './components/Home';
import { createRootStore } from './stores/rootStore';
import { StoreProvider } from './stores/useStore';

const store = createRootStore();

const App: React.FC = () => (
    <StoreProvider store={store}>
        <Card style={{ padding: 16 }}>
            <Home />
        </Card>
    </StoreProvider>
);

export default App;