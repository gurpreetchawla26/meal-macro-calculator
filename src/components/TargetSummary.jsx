export default function TargetSummary({ targets }) {
  if (!targets) {
    return (
      <section className="card">
        <h2>Daily Targets</h2>
        <p className="muted">Fill in your metrics to see your daily calorie and macro targets.</p>
      </section>
    );
  }

  const stats = [
    { label: 'BMR', value: targets.bmr, unit: 'kcal' },
    { label: 'TDEE', value: targets.tdee, unit: 'kcal' },
    { label: 'Calories', value: targets.calories, unit: 'kcal', highlight: true },
    { label: 'Protein', value: targets.protein, unit: 'g' },
    { label: 'Carbs', value: targets.carbs, unit: 'g' },
    { label: 'Fat', value: targets.fat, unit: 'g' },
  ];

  return (
    <section className="card">
      <h2>Daily Targets</h2>
      <div className="stat-grid">
        {stats.map(({ label, value, unit, highlight }) => (
          <div key={label} className={`stat ${highlight ? 'stat-highlight' : ''}`}>
            <span className="stat-label">{label}</span>
            <span className="stat-value">
              {value} <small>{unit}</small>
            </span>
          </div>
        ))}
      </div>
    </section>
  );
}
