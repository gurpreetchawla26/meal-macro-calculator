# Meal & Macro Calculator

A lightweight React app that estimates your daily calorie and macronutrient targets and lets you build a day of meals to see how close you are to hitting them.

## Features

- **User Metrics Form**: enter age, gender, height (cm), weight (kg), activity level (1.2–1.9) and goal (lose / maintain / gain).
- **Target calculation**
  - BMR via **Mifflin-St Jeor**:
    - Male: `10 × kg + 6.25 × cm − 5 × age + 5`
    - Female: `10 × kg + 6.25 × cm − 5 × age − 161`
  - TDEE = BMR × activity multiplier
  - Calories = TDEE − 500 (lose) / ±0 (maintain) / +300 (gain), with a minimum of 1500 kcal for men and 1200 kcal for women
  - Protein = 2.0 / 1.6 / 1.8 g per kg (lose / maintain / gain), fat = 25% of calories, carbs = remaining calories
- **Local food dataset**: 12 common foods (per 100 g) in `src/data/foods.js`.
- **Daily Meal Builder**: search or pick a food, enter grams, add it to the list, remove items, and watch the totals and progress bars update as you go. Bars turn red when you go over a target.

## Tech Stack

- React 18 (only `useState` and `useMemo`, no state management library)
- Vite 5 (dev server and build)
- Plain CSS (no UI framework)

## Project Structure

```
src/
├── App.jsx                  # Top-level layout; owns the `targets` state
├── App.css                  # All styles
├── main.jsx                 # React entry point
├── components/
│   ├── MetricsForm.jsx      # User input + validation
│   ├── TargetSummary.jsx    # Shows BMR, TDEE and macro targets
│   ├── MealBuilder.jsx      # Food search/select, grams input, meal list
│   └── MealTotals.jsx       # Consumed vs. target progress bars
├── data/
│   └── foods.js             # Static food dataset (per 100 g)
└── utils/
    └── macros.js            # Pure calculation functions
```

## Getting Started

Requires Node.js 18 or newer.

```bash
npm install
npm run dev        # open the printed localhost URL
npm run build      # production build in dist/
```

## Known Gaps & Future Improvements

- **No persistence**: the meal list and targets reset when you refresh. Saving them to `localStorage` would fix this.
- **Small, static dataset**: the 12 foods are approximate averages. A nutrition API (e.g. USDA FoodData Central) or a "custom food" form could replace it.
- **One generic macro split**: the fixed protein and fat ratios don't account for body fat %, training type or dietary preferences such as keto or vegan.
- **Binary gender field**: Mifflin-St Jeor only defines male and female constants. A more inclusive approach needs a clearly documented alternative.
- **Adults only**: inputs are limited to ages 18–80 because the formula isn't validated for children or teenagers.
- **No automated tests**: `utils/macros.js` is made of pure functions, so adding Vitest unit tests would be easy.
- **Single day only**: there's no meal grouping (breakfast, lunch and so on), history or weekly view.
- **Accessibility**: there's basic labelling, but the app hasn't been audited with a screen reader.
- **Not medical advice**: these are estimates. Users with medical conditions should consult a professional.
