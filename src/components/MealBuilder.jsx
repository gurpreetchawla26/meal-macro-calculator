import { useMemo, useState } from 'react';
import { FOODS } from '../data/foods.js';
import { macrosForPortion, sumMacros } from '../utils/macros.js';
import MealTotals from './MealTotals.jsx';

const MAX_GRAMS = 2000;

const filterFoods = (query) =>
  FOODS.filter((food) => food.name.toLowerCase().includes(query.trim().toLowerCase()));

export default function MealBuilder({ targets }) {
  const [search, setSearch] = useState('');
  const [selectedFoodId, setSelectedFoodId] = useState(String(FOODS[0].id));
  const [grams, setGrams] = useState('100');
  const [entries, setEntries] = useState([]);
  const [error, setError] = useState('');

  const filteredFoods = useMemo(() => filterFoods(search), [search]);

  // Recomputed on every change so totals update in real time.
  const totals = useMemo(() => sumMacros(entries), [entries]);

  const handleSearchChange = (event) => {
    const value = event.target.value;
    setSearch(value);
    // Keep the dropdown selection valid when the filter hides the current choice.
    const matches = filterFoods(value);
    if (matches.length && !matches.some((f) => String(f.id) === selectedFoodId)) {
      setSelectedFoodId(String(matches[0].id));
    }
  };

  const handleAdd = (event) => {
    event.preventDefault();
    const food = FOODS.find((f) => String(f.id) === selectedFoodId);
    const amount = Number(grams);

    if (!food || !filteredFoods.includes(food)) {
      setError('Please select a food item.');
      return;
    }
    if (!amount || amount <= 0 || amount > MAX_GRAMS) {
      setError(`Weight must be between 1 and ${MAX_GRAMS} g.`);
      return;
    }

    setError('');
    setEntries((prev) => [...prev, { entryId: crypto.randomUUID(), food, grams: amount }]);
  };

  const handleRemove = (entryId) => {
    setEntries((prev) => prev.filter((entry) => entry.entryId !== entryId));
  };

  return (
    <section className="card">
      <h2>2. Daily Meal Builder</h2>

      <form className="meal-form" onSubmit={handleAdd} noValidate>
        <label>
          Search
          <input
            type="text"
            placeholder="e.g. paneer"
            value={search}
            onChange={handleSearchChange}
          />
        </label>

        <label>
          Food
          <select value={selectedFoodId} onChange={(e) => setSelectedFoodId(e.target.value)}>
            {filteredFoods.length === 0 && <option value="">No matches</option>}
            {filteredFoods.map((food) => (
              <option key={food.id} value={food.id}>
                {food.name} ({food.calories} kcal/100g)
              </option>
            ))}
          </select>
        </label>

        <label>
          Weight (g)
          <input
            type="number"
            min="1"
            max={MAX_GRAMS}
            value={grams}
            onChange={(e) => setGrams(e.target.value)}
          />
        </label>

        <button type="submit" className="btn-primary">Add</button>
      </form>

      {error && <p className="error" role="alert">{error}</p>}

      {entries.length === 0 ? (
        <p className="muted">No foods added yet.</p>
      ) : (
        <>
          <table className="meal-table">
            <thead>
              <tr>
                <th>Food</th>
                <th>Grams</th>
                <th>kcal</th>
                <th>P (g)</th>
                <th>C (g)</th>
                <th>F (g)</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {entries.map(({ entryId, food, grams: portionGrams }) => {
                const portion = macrosForPortion(food, portionGrams);
                return (
                  <tr key={entryId}>
                    <td>{food.name}</td>
                    <td>{portionGrams}</td>
                    <td>{Math.round(portion.calories)}</td>
                    <td>{portion.protein.toFixed(1)}</td>
                    <td>{portion.carbs.toFixed(1)}</td>
                    <td>{portion.fat.toFixed(1)}</td>
                    <td>
                      <button
                        type="button"
                        className="btn-link"
                        onClick={() => handleRemove(entryId)}
                        aria-label={`Remove ${food.name}`}
                      >
                        Remove
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <button type="button" className="btn-secondary" onClick={() => setEntries([])}>
            Clear All
          </button>
        </>
      )}

      <h3>Totals vs. Targets</h3>
      {!targets && <p className="muted">Calculate your targets above to compare progress.</p>}
      <MealTotals totals={totals} targets={targets} />
    </section>
  );
}
