export type FireLifestyle = 'minimal' | 'moderate' | 'comfortable' | 'luxury' | 'not-sure'
export type FireWorkPreference = 'quit-completely' | 'part-time' | 'coast' | 'flexible' | 'not-sure'
export type FireTimeline = 'within-5' | '5-10' | '10-20' | '20-plus' | 'not-sure'
export type FirePrimaryGoal = 'retire-early' | 'financial-security' | 'maintain-lifestyle' | 'flexibility' | 'not-sure'

export interface QuizAnswers {
  lifestyle?: FireLifestyle
  workPreference?: FireWorkPreference
  timeline?: FireTimeline
  primaryGoal?: FirePrimaryGoal
  currentAge?: number
  retirementAge?: number
  currentSavings?: number
  annualIncome?: number
  annualExpenses?: number
}

export interface FireQuizMatch {
  path: string
  title: string
  reason: string
  description: string
  benefits: string[]
  reasonIds: string[]
  score: number
}

export interface FireQuizRecommendation {
  primary: FireQuizMatch
  alternatives: FireQuizMatch[]
  confidence: 'low' | 'medium' | 'high'
}

interface Candidate {
  path: string
  title: string
  description: string
  benefits: string[]
  score: number
  reasonIds: string[]
}

const tieBreakOrder = ['/standard', '/coast', '/barista', '/reverse', '/lean', '/fat']

const pathDefinitions: Omit<Candidate, 'score' | 'reasonIds'>[] = [
  {
    path: '/standard',
    title: 'Standard FIRE',
    description: 'Build a complete retirement portfolio using a balanced spending and withdrawal plan.',
    benefits: ['Plan for full financial independence', 'Balance lifestyle and timing', 'Adjust assumptions as life changes'],
  },
  {
    path: '/lean',
    title: 'Lean FIRE',
    description: 'Reach financial independence with intentional spending and a smaller portfolio target.',
    benefits: ['Lower the portfolio target', 'Prioritize an earlier timeline', 'Keep spending intentional'],
  },
  {
    path: '/fat',
    title: 'Fat FIRE',
    description: 'Build a larger portfolio target to support more spending and additional financial margin.',
    benefits: ['Maintain a higher-spending lifestyle', 'Create a larger market buffer', 'Preserve room for travel and family goals'],
  },
  {
    path: '/barista',
    title: 'Barista FIRE',
    description: 'Blend portfolio income with part-time work to leave full-time work sooner.',
    benefits: ['Leave full-time work earlier', 'Cover some expenses with earned income', 'Keep flexibility and social connection'],
  },
  {
    path: '/coast',
    title: 'Coast FIRE',
    description: "Let invested savings grow toward retirement while current work covers today's expenses.",
    benefits: ['Front-load retirement savings', 'Give compound growth more time', 'Create room for lower-stress work'],
  },
  {
    path: '/reverse',
    title: 'Reverse FIRE',
    description: 'Work backward from a target timeline to find the savings required to reach it.',
    benefits: ['Set a clear savings target', 'Compare timeline and contribution tradeoffs', 'Plan around a firm deadline'],
  },
]

const reasonText: Record<string, string> = {
  'balanced-lifestyle': 'your balanced lifestyle goal',
  'comfortable-lifestyle': 'your preference for comfort without the highest spending target',
  'full-retirement': 'your goal of fully leaving paid work',
  'security-first': 'your focus on long-term financial security',
  'balanced-timeline': 'your flexible mid-to-long-term timeline',
  'minimal-lifestyle': 'your lower-spending lifestyle',
  'early-priority': 'your priority to reach financial independence sooner',
  'lower-expenses': 'the lower annual expenses you shared',
  'luxury-lifestyle': 'your higher-spending lifestyle goal',
  'maintain-lifestyle': 'your priority to preserve your lifestyle',
  'higher-expenses': 'the higher annual expenses you shared',
  'part-time-work': 'your interest in part-time work',
  'work-flexibility': 'your desire to keep work options open',
  'near-term-transition': 'your near-term transition goal',
  'coast-work': 'your preference to let investments grow while work covers current expenses',
  'long-horizon': 'your longer time horizon',
  'deadline-focus': 'your firm, near-term timeline',
  'retire-early': 'your goal to retire as early as practical',
  'balanced-start': 'a balanced starting point while you refine your preferences',
}

export function recommendFirePaths(answers: QuizAnswers): FireQuizRecommendation {
  const candidates = new Map<string, Candidate>(
    pathDefinitions.map(definition => [
      definition.path,
      { ...definition, score: 0, reasonIds: [] as string[] },
    ]),
  )

  const add = (path: string, score: number, reasonId: string) => {
    const candidate = candidates.get(path)
    if (!candidate) throw new Error(`Unknown FIRE path: ${path}`)
    candidate.score += score
    if (!candidate.reasonIds.includes(reasonId)) candidate.reasonIds.push(reasonId)
  }

  add('/standard', 1, 'balanced-start')

  if (answers.lifestyle === 'minimal') add('/lean', 6, 'minimal-lifestyle')
  if (answers.lifestyle === 'moderate') add('/standard', 4, 'balanced-lifestyle')
  if (answers.lifestyle === 'comfortable') {
    add('/standard', 3, 'comfortable-lifestyle')
    add('/fat', 1, 'comfortable-lifestyle')
  }
  if (answers.lifestyle === 'luxury') add('/fat', 6, 'luxury-lifestyle')

  if (answers.workPreference === 'quit-completely') {
    add('/standard', 2, 'full-retirement')
    add('/reverse', 1, 'full-retirement')
  }
  if (answers.workPreference === 'part-time') add('/barista', 7, 'part-time-work')
  if (answers.workPreference === 'coast') add('/coast', 7, 'coast-work')
  if (answers.workPreference === 'flexible') {
    add('/barista', 2, 'work-flexibility')
    add('/coast', 2, 'work-flexibility')
    add('/standard', 1, 'work-flexibility')
  }

  if (answers.timeline === 'within-5') {
    add('/reverse', 5, 'deadline-focus')
    add('/barista', 2, 'near-term-transition')
    add('/lean', 1, 'near-term-transition')
  }
  if (answers.timeline === '5-10') {
    add('/reverse', 3, 'deadline-focus')
    add('/barista', 1, 'near-term-transition')
    add('/lean', 1, 'near-term-transition')
  }
  if (answers.timeline === '10-20') {
    add('/standard', 2, 'balanced-timeline')
    add('/coast', 1, 'long-horizon')
  }
  if (answers.timeline === '20-plus') {
    add('/coast', 3, 'long-horizon')
    add('/standard', 1, 'balanced-timeline')
  }

  if (answers.primaryGoal === 'retire-early') {
    add('/reverse', 4, 'retire-early')
    add('/lean', 2, 'early-priority')
  }
  if (answers.primaryGoal === 'financial-security') add('/standard', 4, 'security-first')
  if (answers.primaryGoal === 'maintain-lifestyle') {
    add('/fat', 2, 'maintain-lifestyle')
    add('/standard', 2, 'maintain-lifestyle')
  }
  if (answers.primaryGoal === 'flexibility') {
    add('/coast', 2, 'work-flexibility')
    add('/barista', 2, 'work-flexibility')
  }

  if (answers.annualExpenses !== undefined && answers.annualExpenses < 40_000) {
    add('/lean', 2, 'lower-expenses')
  } else if (answers.annualExpenses !== undefined && answers.annualExpenses >= 100_000) {
    add('/fat', 3, 'higher-expenses')
  }

  const ranked = [...candidates.values()]
    .sort((a, b) => b.score - a.score || tieBreakOrder.indexOf(a.path) - tieBreakOrder.indexOf(b.path))
    .map(candidate => {
      const meaningfulReasons = candidate.reasonIds
        .filter(reasonId => reasonId !== 'balanced-start' || candidate.reasonIds.length === 1)
        .slice(0, 2)
        .map(reasonId => reasonText[reasonId])
      const reason = meaningfulReasons.length === 0
        ? 'This is a useful path to compare with your leading match.'
        : meaningfulReasons.length === 1
          ? `This path aligns with ${meaningfulReasons[0]}.`
          : `This path aligns with ${meaningfulReasons[0]} and ${meaningfulReasons[1]}.`
      return { ...candidate, reason }
    })

  const knownCoreAnswers = [
    answers.lifestyle && answers.lifestyle !== 'not-sure',
    answers.workPreference && answers.workPreference !== 'not-sure',
    answers.timeline && answers.timeline !== 'not-sure',
    answers.primaryGoal && answers.primaryGoal !== 'not-sure',
  ].filter(Boolean).length
  const margin = ranked[0].score - ranked[1].score
  const confidence = knownCoreAnswers >= 4 && margin >= 3
    ? 'high'
    : knownCoreAnswers >= 2 && margin >= 2
      ? 'medium'
      : 'low'

  return { primary: ranked[0], alternatives: ranked.slice(1, 3), confidence }
}
