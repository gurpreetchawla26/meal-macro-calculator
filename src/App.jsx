import { useState } from 'react';
import MetricsForm from './components/MetricsForm.jsx';
import TargetSummary from './components/TargetSummary.jsx';
import MealBuilder from './components/MealBuilder.jsx';
import { calculateTargets } from './utils/macros.js';

export default function App() {
  // Targets are lifted here so both the summary and meal builder can read them.
  const [targets, setTargets] = useState(null);

  return (
    <div className="app">
      <header className="app-header">
        <h1>Meal &amp; Macro Calculator</h1>
        <p className="muted">Estimate your daily needs and plan meals to hit them.</p>
      </header>

      <main className="layout">
        <div className="column">
          <MetricsForm onCalculate={(metrics) => setTargets(calculateTargets(metrics))} />
          <TargetSummary targets={targets} />
        </div>
        <div className="column">
          <MealBuilder targets={targets} />
        </div>
      </main>

      <footer className="app-footer muted">
        Estimates only — not medical advice.
      </footer>
    </div>
  );
}
