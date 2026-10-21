import { useState } from 'react';
import { ACTIVITY_LEVELS, GOALS } from '../utils/macros.js';

const INITIAL_FORM = {
  age: '',
  gender: 'male',
  heightCm: '',
  weightKg: '',
  activityLevel: '1.2',
  goal: 'maintain',
};

// Mifflin-St Jeor was validated on adults, so inputs are kept to realistic adult ranges.
const LIMITS = {
  age: { min: 18, max: 80, label: 'Age' },
  heightCm: { min: 120, max: 230, label: 'Height' },
  weightKg: { min: 30, max: 250, label: 'Weight' },
};

function validate(form) {
  for (const [field, { min, max, label }] of Object.entries(LIMITS)) {
    const value = Number(form[field]);
    if (form[field] === '' || Number.isNaN(value) || value < min || value > max) {
      return `${label} must be between ${min} and ${max}.`;
    }
  }
  return '';
}

export default function MetricsForm({ onCalculate }) {
  const [form, setForm] = useState(INITIAL_FORM);
  const [error, setError] = useState('');

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    const message = validate(form);
    setError(message);
    if (message) return;

    onCalculate({
      age: Number(form.age),
      gender: form.gender,
      heightCm: Number(form.heightCm),
      weightKg: Number(form.weightKg),
      activityLevel: Number(form.activityLevel),
      goal: form.goal,
    });
  };

  return (
    <form className="card form" onSubmit={handleSubmit} noValidate>
      <h2>1. Your Metrics</h2>

      <div className="form-grid">
        <label>
          Age (years)
          <input type="number" name="age" value={form.age} onChange={handleChange} min="18" max="80" />
        </label>

        <label>
          Gender
          <select name="gender" value={form.gender} onChange={handleChange}>
            <option value="male">Male</option>
            <option value="female">Female</option>
          </select>
        </label>

        <label>
          Height (cm)
          <input type="number" name="heightCm" value={form.heightCm} onChange={handleChange} min="120" max="230" />
        </label>

        <label>
          Weight (kg)
          <input type="number" name="weightKg" value={form.weightKg} onChange={handleChange} min="30" max="250" step="0.1" />
        </label>

        <label>
          Activity Level
          <select name="activityLevel" value={form.activityLevel} onChange={handleChange}>
            {ACTIVITY_LEVELS.map(({ value, label }) => (
              <option key={value} value={value}>
                {value} – {label}
              </option>
            ))}
          </select>
        </label>

        <label>
          Goal
          <select name="goal" value={form.goal} onChange={handleChange}>
            {GOALS.map(({ value, label }) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {error && <p className="error" role="alert">{error}</p>}

      <button type="submit" className="btn-primary">Calculate Targets</button>
    </form>
  );
}
