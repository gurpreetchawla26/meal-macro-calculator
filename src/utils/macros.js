// Pure calculation helpers — no React here, so they are easy to unit test.

export const ACTIVITY_LEVELS = [
  { value: 1.2, label: 'Sedentary (little or no exercise)' },
  { value: 1.375, label: 'Lightly active (1–3 days/week)' },
  { value: 1.55, label: 'Moderately active (3–5 days/week)' },
  { value: 1.725, label: 'Very active (6–7 days/week)' },
  { value: 1.9, label: 'Extra active (physical job + training)' },
];

export const GOALS = [
  { value: 'lose', label: 'Lose weight' },
  { value: 'maintain', label: 'Maintain weight' },
  { value: 'gain', label: 'Gain weight' },
];

// Daily kcal offset from TDEE for each goal.
const GOAL_CALORIE_ADJUSTMENT = { lose: -500, maintain: 0, gain: 300 };

// Grams of protein per kg of body weight for each goal.
const PROTEIN_PER_KG = { lose: 2.0, maintain: 1.6, gain: 1.8 };

const FAT_CALORIE_SHARE = 0.25;
const KCAL_PER_GRAM = { protein: 4, carbs: 4, fat: 9 };

// Commonly cited safety floors so a deficit never drops intake too low.
const MIN_DAILY_CALORIES = { male: 1500, female: 1200 };

/** Mifflin-St Jeor BMR in kcal/day. */
export function calculateBMR({ age, gender, heightCm, weightKg }) {
  const base = 10 * weightKg + 6.25 * heightCm - 5 * age;
  return gender === 'male' ? base + 5 : base - 161;
}

/** Returns BMR, TDEE and daily calorie/macro targets (all rounded). */
export function calculateTargets(metrics) {
  const bmr = calculateBMR(metrics);
  const tdee = bmr * metrics.activityLevel;
  const calories = Math.max(
    tdee + GOAL_CALORIE_ADJUSTMENT[metrics.goal],
    MIN_DAILY_CALORIES[metrics.gender]
  );

  const protein = metrics.weightKg * PROTEIN_PER_KG[metrics.goal];
  const fat = (calories * FAT_CALORIE_SHARE) / KCAL_PER_GRAM.fat;
  // Carbs fill whatever calories are left after protein and fat.
  const carbCalories = calories - protein * KCAL_PER_GRAM.protein - fat * KCAL_PER_GRAM.fat;
  const carbs = Math.max(carbCalories / KCAL_PER_GRAM.carbs, 0);

  return {
    bmr: Math.round(bmr),
    tdee: Math.round(tdee),
    calories: Math.round(calories),
    protein: Math.round(protein),
    carbs: Math.round(carbs),
    fat: Math.round(fat),
  };
}

/** Scales a food's per-100 g values to the given portion size. */
export function macrosForPortion(food, grams) {
  const factor = grams / 100;
  return {
    calories: food.calories * factor,
    protein: food.protein * factor,
    carbs: food.carbs * factor,
    fat: food.fat * factor,
  };
}

/** Sums macros across meal entries shaped like { food, grams }. */
export function sumMacros(entries) {
  return entries.reduce(
    (totals, { food, grams }) => {
      const portion = macrosForPortion(food, grams);
      return {
        calories: totals.calories + portion.calories,
        protein: totals.protein + portion.protein,
        carbs: totals.carbs + portion.carbs,
        fat: totals.fat + portion.fat,
      };
    },
    { calories: 0, protein: 0, carbs: 0, fat: 0 }
  );
}
