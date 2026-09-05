import { vi } from 'vitest';
import React from 'react';
import '@testing-library/jest-dom';

// Polyfill ResizeObserver for Recharts in jsdom
class ResizeObserverMock {
  observe() {}
  unobserve() {}
  disconnect() {}
}

if (!globalThis.ResizeObserver) {
  globalThis.ResizeObserver = ResizeObserverMock as any;
}

// Mock Recharts ResponsiveContainer to render children in JSDOM
vi.mock('recharts', async (importOriginal) => {
  const original = await importOriginal<Record<string, any>>();
  return {
    ...original,
    ResponsiveContainer: ({ children }: any) =>
      React.createElement(
        'div',
        { className: 'recharts-responsive-container', style: { width: 400, height: 300 } },
        children
      ),
  };
});
