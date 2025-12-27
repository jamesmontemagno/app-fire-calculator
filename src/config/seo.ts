/**
 * SEO metadata for all calculator pages
 * Centralized SEO configuration for consistent meta tags
 */

export interface PageSEO {
  title: string
  description: string
  keywords: string
  canonicalPath: string
}

export const calculatorSEO: Record<string, PageSEO> = {
  standard: {
    title: 'Standard FIRE Calculator - Calculate Your Financial Independence Number',
    description: 'Free Standard FIRE calculator using the 25x rule. Calculate your FIRE number based on the 4% rule and see how long until you reach financial independence. 100% private, works offline.',
    keywords: 'standard FIRE calculator, 25x rule calculator, 4% rule, FIRE number, financial independence calculator, early retirement, trinity study',
    canonicalPath: '/standard',
  },
  coast: {
    title: 'Coast FIRE Calculator - Calculate Your Coast FI Number',
    description: 'Free Coast FIRE calculator to determine how much you need to save now so compound growth handles the rest. Calculate your coast number and see when you can stop saving for retirement.',
    keywords: 'coast FIRE calculator, coast FI, compound interest calculator, retirement planning, semi-retirement calculator',
    canonicalPath: '/coast',
  },
  lean: {
    title: 'Lean FIRE Calculator - Minimalist Early Retirement Planning',
    description: 'Free Lean FIRE calculator for minimalist lifestyle planning. Calculate your FIRE number with lower expenses ($40k or less annually) and retire earlier with a frugal approach.',
    keywords: 'lean FIRE calculator, frugal retirement, minimalist FIRE, low cost retirement, early retirement on a budget',
    canonicalPath: '/lean',
  },
  fat: {
    title: 'Fat FIRE Calculator - Luxury Early Retirement Planning',
    description: 'Free Fat FIRE calculator for planning a luxurious early retirement. Calculate your FIRE number for a comfortable lifestyle ($100k+ annually) without financial compromises.',
    keywords: 'fat FIRE calculator, luxury retirement, high income FIRE, comfortable retirement, affluent early retirement',
    canonicalPath: '/fat',
  },
  barista: {
    title: 'Barista FIRE Calculator - Part-Time Work & Early Retirement',
    description: 'Free Barista FIRE calculator to blend part-time income with portfolio withdrawals. Calculate how much you need to semi-retire and work flexibly while maintaining financial independence.',
    keywords: 'barista FIRE calculator, semi-retirement, part-time work retirement, flexible retirement, side income FIRE',
    canonicalPath: '/barista',
  },
  reverse: {
    title: 'Reverse FIRE Calculator - Work Backwards From Retirement Age',
    description: 'Free Reverse FIRE calculator - set your target retirement age and discover how much you need to save monthly. Work backwards from your goal to create an actionable savings plan.',
    keywords: 'reverse FIRE calculator, retirement savings calculator, monthly savings goal, target retirement age, savings rate calculator',
    canonicalPath: '/reverse',
  },
  withdrawal: {
    title: 'Withdrawal Rate Calculator - Safe Retirement Withdrawal Rates',
    description: 'Free withdrawal rate calculator to test your portfolio longevity. Calculate safe withdrawal rates, see how long your money will last, and plan for sustainable retirement income.',
    keywords: 'withdrawal rate calculator, safe withdrawal rate, 4% rule, retirement income calculator, portfolio longevity, retirement sustainability',
    canonicalPath: '/withdrawal',
  },
  'savings-rate': {
    title: 'Savings Rate Calculator - Calculate Time to Financial Independence',
    description: 'Free savings rate calculator showing how your savings rate impacts your time to FIRE. The most important metric for early retirement - see exactly when you can achieve financial independence.',
    keywords: 'savings rate calculator, time to FIRE, financial independence timeline, retirement savings rate, early retirement calculator',
    canonicalPath: '/savings-rate',
  },
  'debt-payoff': {
    title: 'Debt Payoff Calculator - Snowball vs Avalanche Method',
    description: 'Free debt payoff calculator comparing Snowball and Avalanche strategies. Calculate payoff timelines, see interest savings, and plan extra payments to eliminate debt faster.',
    keywords: 'debt payoff calculator, debt snowball calculator, debt avalanche calculator, debt elimination, pay off debt faster, debt free calculator',
    canonicalPath: '/debt-payoff',
  },
  healthcare: {
    title: 'Healthcare Gap Calculator - Early Retirement Healthcare Costs',
    description: 'Free healthcare gap calculator for early retirees. Estimate healthcare costs between early retirement and Medicare eligibility at 65. Essential planning for US-based FIRE seekers.',
    keywords: 'healthcare gap calculator, early retirement healthcare, pre-Medicare coverage, healthcare costs, ACA marketplace, retirement healthcare planning',
    canonicalPath: '/healthcare',
  },
  books: {
    title: 'Best FIRE Books - Recommended Financial Independence Reading',
    description: 'Curated list of the best FIRE books to accelerate your financial independence journey. Essential reading on investing, personal finance, early retirement, and wealth building.',
    keywords: 'FIRE books, financial independence books, early retirement books, personal finance books, investing books, wealth building books',
    canonicalPath: '/books',
  },
  apps: {
    title: 'Recommended FIRE Apps - Financial Independence Tools & Software',
    description: 'Curated list of the best FIRE apps to help you track spending, manage budgets, and achieve financial independence. Essential tools to complement your FIRE calculators.',
    keywords: 'FIRE apps, financial independence apps, budgeting apps, expense tracking apps, personal finance apps, money management apps',
    canonicalPath: '/apps',
  },
  quiz: {
    title: 'FIRE Calculator Quiz - Find Your Perfect FIRE Path',
    description: 'Take our free FIRE quiz to discover which calculator and strategy fits your situation. Answer a few questions and get personalized calculator recommendations with pre-filled values.',
    keywords: 'FIRE quiz, which FIRE calculator, FIRE personality quiz, financial independence quiz, early retirement quiz',
    canonicalPath: '/quiz',
  },
}
