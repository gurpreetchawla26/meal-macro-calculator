const MACROS = [
  { key: 'calories', label: 'Calories', unit: 'kcal' },
  { key: 'protein', label: 'Protein', unit: 'g' },
  { key: 'carbs', label: 'Carbs', unit: 'g' },
  { key: 'fat', label: 'Fat', unit: 'g' },
];

/** Shows consumed vs. target for each macro with a simple progress bar. */
export default function MealTotals({ totals, targets }) {
  return (
    <div className="totals">
      {MACROS.map(({ key, label, unit }) => {
        const consumed = Math.round(totals[key]);
        const target = targets?.[key];
        const percent = target ? Math.round((consumed / target) * 100) : 0;
        const isOver = target && consumed > target;

        return (
          <div key={key} className="total-row">
            <div className="total-header">
              <span>{label}</span>
              <span className={isOver ? 'over' : ''}>
                {consumed} / {target ?? '—'} {unit}
                {target ? ` (${percent}%)` : ''}
              </span>
            </div>
            <div className="progress">
              <div
                className={`progress-fill ${isOver ? 'progress-over' : ''}`}
                style={{ width: `${Math.min(percent, 100)}%` }}
              />
            </div>
          </div>
        );
      })}
    </div>
  );
}
