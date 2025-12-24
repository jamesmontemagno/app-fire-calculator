import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardHeader, CardContent } from '../components/ui'

interface QuizAnswers {
  currentAge?: number
  retirementAge?: number
  currentSavings?: number
  annualIncome?: number
  annualExpenses?: number
  lifestyle?: 'minimal' | 'moderate' | 'comfortable' | 'luxury'
  workPreference?: 'quit-completely' | 'part-time' | 'flexible' | 'coast'
  riskTolerance?: 'conservative' | 'moderate' | 'aggressive'
  primaryGoal?: 'retire-early' | 'financial-security' | 'maintain-lifestyle' | 'flexibility'
}

interface Recommendation {
  path: string
  title: string
  icon: string
  reason: string
  description: string
  benefits: string[]
}

export default function FIREQuiz() {
  const navigate = useNavigate()
  const [step, setStep] = useState(0)
  const [answers, setAnswers] = useState<QuizAnswers>({})
  const [recommendation, setRecommendation] = useState<Recommendation | null>(null)

  const questions = [
    {
      id: 'currentAge',
      title: 'How old are you?',
      subtitle: 'This helps us understand your timeline',
      type: 'number',
      placeholder: '30',
      min: 18,
      max: 80,
    },
    {
      id: 'retirementAge',
      title: 'When do you want to achieve financial independence?',
      subtitle: 'Your target age for FIRE',
      type: 'number',
      placeholder: '50',
      min: (answers.currentAge || 18) + 1,
      max: 80,
    },
    {
      id: 'currentSavings',
      title: 'How much do you currently have saved/invested?',
      subtitle: 'Include all retirement accounts and investments',
      type: 'currency',
      placeholder: '100,000',
    },
    {
      id: 'annualIncome',
      title: 'What\'s your current annual household income?',
      subtitle: 'Before taxes',
      type: 'currency',
      placeholder: '80,000',
    },
    {
      id: 'annualExpenses',
      title: 'What are your current annual expenses?',
      subtitle: 'Or what you expect to spend in retirement',
      type: 'currency',
      placeholder: '50,000',
    },
    {
      id: 'lifestyle',
      title: 'What lifestyle do you want in retirement?',
      subtitle: 'This impacts your target savings amount',
      type: 'choice',
      choices: [
        { value: 'minimal', label: 'Minimal/Frugal', desc: 'Living simply, $30K-$40K/year' },
        { value: 'moderate', label: 'Moderate', desc: 'Comfortable basics, $40K-$70K/year' },
        { value: 'comfortable', label: 'Comfortable', desc: 'No major sacrifices, $70K-$100K/year' },
        { value: 'luxury', label: 'Luxury/Fat', desc: 'High-end lifestyle, $100K+/year' },
      ],
    },
    {
      id: 'workPreference',
      title: 'What\'s your ideal work situation after reaching FI?',
      subtitle: 'How you want to spend your time',
      type: 'choice',
      choices: [
        { value: 'quit-completely', label: 'Quit Completely', desc: 'Never work again' },
        { value: 'part-time', label: 'Part-Time Work', desc: 'Work for benefits or extra income' },
        { value: 'coast', label: 'Coast Mode', desc: 'Low-stress job covering expenses' },
        { value: 'flexible', label: 'Stay Flexible', desc: 'Keep options open' },
      ],
    },
    {
      id: 'primaryGoal',
      title: 'What\'s most important to you?',
      subtitle: 'Your primary motivation for FIRE',
      type: 'choice',
      choices: [
        { value: 'retire-early', label: 'Retire ASAP', desc: 'Leave workforce as early as possible' },
        { value: 'financial-security', label: 'Financial Security', desc: 'Peace of mind and freedom' },
        { value: 'maintain-lifestyle', label: 'Maintain Lifestyle', desc: 'Retire without sacrifice' },
        { value: 'flexibility', label: 'Flexibility', desc: 'Options and work-life balance' },
      ],
    },
  ]

  const calculateRecommendation = (): Recommendation => {
    const { lifestyle, workPreference, primaryGoal, currentAge, retirementAge, annualExpenses } = answers
    
    // Calculate years to retirement
    const yearsToFIRE = (retirementAge || 65) - (currentAge || 30)
    
    // Determine expense level
    const expenseLevel = annualExpenses || 50000

    // Lean FIRE logic
    if (lifestyle === 'minimal' || (expenseLevel < 40000 && primaryGoal === 'retire-early')) {
      return {
        path: '/lean',
        title: 'Lean FIRE',
        icon: '🌿',
        reason: 'Your minimalist lifestyle and lower expenses make Lean FIRE achievable',
        description: 'Achieve FI faster by living frugally. Requires less savings but demands lifestyle discipline.',
        benefits: [
          'Retire years earlier than traditional FIRE',
          'Need less total savings to reach your goal',
          'Forces intentional spending habits',
          'Freedom with minimalist lifestyle',
        ],
      }
    }

    // Fat FIRE logic
    if (lifestyle === 'luxury' || (expenseLevel >= 100000 && primaryGoal === 'maintain-lifestyle')) {
      return {
        path: '/fat',
        title: 'Fat FIRE',
        icon: '💎',
        reason: 'Your desire for a comfortable lifestyle without compromise aligns with Fat FIRE',
        description: 'Achieve FI while maintaining a luxurious or upper-middle-class lifestyle. Requires more savings but no sacrifices.',
        benefits: [
          'Retire without lifestyle changes',
          'Extra buffer for market downturns',
          'Travel, dining, and experiences without worry',
          'Help family and leave a legacy',
        ],
      }
    }

    // Barista FIRE logic
    if (workPreference === 'part-time' || (yearsToFIRE < 10 && primaryGoal === 'flexibility')) {
      return {
        path: '/barista',
        title: 'Barista FIRE',
        icon: '☕',
        reason: 'Your interest in part-time work makes Barista FIRE an ideal stepping stone',
        description: 'Blend portfolio income with part-time work. Reach FI faster while maintaining health benefits and social connections.',
        benefits: [
          'Quit corporate job years earlier',
          'Part-time work covers some expenses',
          'Health insurance from employer',
          'Stay active and socially engaged',
        ],
      }
    }

    // Coast FIRE logic
    if (workPreference === 'coast' || (currentAge || 30) < 35 && yearsToFIRE > 20) {
      return {
        path: '/coast',
        title: 'Coast FIRE',
        icon: '⛵',
        reason: 'Your timeline and age make Coast FIRE a strategic approach',
        description: 'Save aggressively now, then let compound growth do the work. Perfect for young savers wanting flexibility.',
        benefits: [
          'Save hard early, then ease off',
          'Compound growth does the heavy lifting',
          'Take lower-paying but fulfilling work',
          'Reduces pressure in your 30s-40s',
        ],
      }
    }

    // Reverse FIRE logic
    if (primaryGoal === 'retire-early' && yearsToFIRE < 15) {
      return {
        path: '/reverse',
        title: 'Reverse FIRE',
        icon: '🔄',
        reason: 'Your specific retirement age goal calls for a targeted savings strategy',
        description: 'Work backwards from your target age to calculate exactly what you need to save monthly.',
        benefits: [
          'Clear monthly savings target',
          'Goal-oriented approach',
          'Adjust timeline or savings as needed',
          'Perfect for deadline-driven planners',
        ],
      }
    }

    // Savings Rate focus
    if (primaryGoal === 'financial-security') {
      return {
        path: '/savings-rate',
        title: 'Savings Rate Calculator',
        icon: '🧮',
        reason: 'Understanding your savings rate is the foundation for all FIRE paths',
        description: 'Your savings rate is the most important metric in FIRE. This calculator shows exactly how it impacts your timeline.',
        benefits: [
          'See direct impact of saving more',
          'Most important FIRE metric',
          'Works for any income level',
          'Clear path to financial freedom',
        ],
      }
    }

    // Default to Standard FIRE
    return {
      path: '/standard',
      title: 'Standard FIRE',
      icon: '🎯',
      reason: 'The classic FIRE approach fits your balanced goals and timeline',
      description: 'The traditional 25x expenses rule. A proven, balanced approach to financial independence.',
      benefits: [
        'Time-tested 4% withdrawal rule',
        'Balanced approach for most people',
        'Comprehensive planning framework',
        'Retire at traditional age or earlier',
      ],
    }
  }

  const handleNext = () => {
    if (step < questions.length - 1) {
      setStep(step + 1)
    } else {
      // Calculate recommendation
      const rec = calculateRecommendation()
      setRecommendation(rec)
    }
  }

  const handlePrevious = () => {
    if (step > 0) {
      setStep(step - 1)
    }
  }

  const handleAnswer = (value: any) => {
    const question = questions[step]
    setAnswers({ ...answers, [question.id]: value })
  }

  const handleGoToCalculator = () => {
    if (!recommendation) return

    // Build query params from answers
    const params = new URLSearchParams()
    if (answers.currentAge) params.set('currentAge', answers.currentAge.toString())
    if (answers.retirementAge) params.set('retirementAge', answers.retirementAge.toString())
    if (answers.currentSavings) params.set('currentSavings', answers.currentSavings.toString())
    if (answers.annualExpenses) params.set('annualExpenses', answers.annualExpenses.toString())
    
    // Calculate annual contribution from income and expenses
    if (answers.annualIncome && answers.annualExpenses) {
      const contribution = Math.max(0, answers.annualIncome - answers.annualExpenses)
      params.set('annualContribution', contribution.toString())
    }

    navigate(`${recommendation.path}?${params.toString()}`)
  }

  const handleStartOver = () => {
    setStep(0)
    setAnswers({})
    setRecommendation(null)
  }

  const currentQuestion = questions[step]
  const currentAnswer = answers[currentQuestion?.id as keyof QuizAnswers]
  const canProceed = currentAnswer !== undefined && currentAnswer !== null

  // Results view
  if (recommendation) {
    return (
      <div className="max-w-3xl mx-auto space-y-6">
        {/* Header */}
        <div className="text-center">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
            Your Recommended Path
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            Based on your answers, here's the best FIRE strategy for you
          </p>
        </div>

        {/* Recommendation Card */}
        <Card className="border-2 border-fire-200 dark:border-fire-800">
          <CardContent className="p-8">
            <div className="text-center mb-6">
              <span className="text-6xl mb-4 block">{recommendation.icon}</span>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
                {recommendation.title}
              </h2>
              <p className="text-lg text-fire-600 dark:text-fire-400 font-medium">
                {recommendation.reason}
              </p>
            </div>

            <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-6 mb-6">
              <p className="text-gray-700 dark:text-gray-300">
                {recommendation.description}
              </p>
            </div>

            <div className="mb-6">
              <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-3">
                Why this path works for you:
              </h3>
              <ul className="space-y-2">
                {recommendation.benefits.map((benefit, index) => (
                  <li key={index} className="flex items-start gap-2 text-gray-600 dark:text-gray-400">
                    <svg className="w-5 h-5 text-green-500 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                    {benefit}
                  </li>
                ))}
              </ul>
            </div>

            <div className="flex gap-4">
              <button
                onClick={handleGoToCalculator}
                className="flex-1 bg-fire-600 hover:bg-fire-700 text-white font-medium py-3 px-6 rounded-lg transition-colors"
              >
                Go to {recommendation.title} Calculator →
              </button>
              <button
                onClick={handleStartOver}
                className="px-6 py-3 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
              >
                Start Over
              </button>
            </div>
          </CardContent>
        </Card>

        {/* Other Options */}
        <Card>
          <CardHeader>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              Want to explore other paths?
            </h3>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
              All FIRE paths can work depending on your circumstances. Feel free to explore multiple calculators to find what resonates with you.
            </p>
            <button
              onClick={() => navigate('/')}
              className="text-fire-600 dark:text-fire-400 hover:underline"
            >
              View All Calculators
            </button>
          </CardContent>
        </Card>
      </div>
    )
  }

  // Quiz view
  return (
    <div className="max-w-2xl mx-auto space-y-6">
      {/* Progress */}
      <div>
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Question {step + 1} of {questions.length}
          </span>
          <span className="text-sm text-gray-500 dark:text-gray-400">
            {Math.round(((step + 1) / questions.length) * 100)}% complete
          </span>
        </div>
        <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
          <div
            className="h-full bg-gradient-to-r from-fire-500 to-fire-600 transition-all duration-300"
            style={{ width: `${((step + 1) / questions.length) * 100}%` }}
          />
        </div>
      </div>

      {/* Question Card */}
      <Card>
        <CardContent className="p-8">
          <div className="mb-6">
            <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
              {currentQuestion.title}
            </h2>
            <p className="text-gray-600 dark:text-gray-400">
              {currentQuestion.subtitle}
            </p>
          </div>

          {/* Input based on type */}
          {currentQuestion.type === 'number' && (
            <div>
              <input
                type="number"
                value={currentAnswer || ''}
                onChange={(e) => handleAnswer(e.target.value ? Number(e.target.value) : undefined)}
                placeholder={currentQuestion.placeholder}
                min={currentQuestion.min}
                max={currentQuestion.max}
                className="w-full px-4 py-3 text-lg border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-fire-500 focus:border-transparent"
              />
            </div>
          )}

          {currentQuestion.type === 'currency' && (
            <div className="relative">
              <span className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500 dark:text-gray-400 text-lg">
                $
              </span>
              <input
                type="number"
                value={currentAnswer || ''}
                onChange={(e) => handleAnswer(e.target.value ? Number(e.target.value) : undefined)}
                placeholder={currentQuestion.placeholder}
                className="w-full pl-8 pr-4 py-3 text-lg border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-fire-500 focus:border-transparent"
              />
            </div>
          )}

          {currentQuestion.type === 'choice' && (
            <div className="space-y-3">
              {currentQuestion.choices?.map((choice) => (
                <button
                  key={choice.value}
                  onClick={() => handleAnswer(choice.value)}
                  className={`w-full text-left p-4 rounded-lg border-2 transition-all ${
                    currentAnswer === choice.value
                      ? 'border-fire-500 bg-fire-50 dark:bg-fire-900/20'
                      : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
                  }`}
                >
                  <div className="font-semibold text-gray-900 dark:text-gray-100">
                    {choice.label}
                  </div>
                  <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                    {choice.desc}
                  </div>
                </button>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Navigation */}
      <div className="flex gap-4">
        <button
          onClick={handlePrevious}
          disabled={step === 0}
          className="px-6 py-3 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          ← Previous
        </button>
        <button
          onClick={handleNext}
          disabled={!canProceed}
          className="flex-1 bg-fire-600 hover:bg-fire-700 text-white font-medium py-3 px-6 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {step === questions.length - 1 ? 'Get Recommendation' : 'Next →'}
        </button>
      </div>
    </div>
  )
}
