import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import SEO from '../components/SEO'
import { Card, CardContent } from '../components/ui'
import { getCalculatorByPath } from '../config/calculators'
import { calculatorSEO } from '../config/seo'
import {
  recommendFirePaths,
  type FireQuizMatch,
  type FireQuizRecommendation,
  type QuizAnswers,
} from '../utils/quizRecommendations'

type ChoiceId = 'lifestyle' | 'workPreference' | 'timeline' | 'primaryGoal' | 'personalize'
type NumericId = 'currentAge' | 'retirementAge' | 'currentSavings' | 'annualIncome' | 'annualExpenses'

interface ChoiceQuestion {
  id: ChoiceId
  type: 'choice'
  title: string
  subtitle: string
  choices: { value: string; label: string; description: string }[]
}

interface NumericQuestion {
  id: NumericId
  type: 'number' | 'currency'
  title: string
  subtitle: string
  placeholder: string
  min: number
  max?: number
}

type Question = ChoiceQuestion | NumericQuestion

const questions: Question[] = [
  {
    id: 'lifestyle',
    type: 'choice',
    title: 'What kind of retirement lifestyle are you planning for?',
    subtitle: 'Choose the closest fit. You can refine the numbers later.',
    choices: [
      { value: 'minimal', label: 'Minimal / frugal', description: 'Keep spending intentionally low.' },
      { value: 'moderate', label: 'Moderate', description: 'Cover comfortable basics with a balanced budget.' },
      { value: 'comfortable', label: 'Comfortable', description: 'Maintain flexibility with few major sacrifices.' },
      { value: 'luxury', label: 'Higher spending', description: 'Plan for more travel, experiences, or financial margin.' },
      { value: 'not-sure', label: 'Not sure yet', description: 'Keep the recommendation broad for now.' },
    ],
  },
  {
    id: 'workPreference',
    type: 'choice',
    title: 'How would you like work to fit into your future?',
    subtitle: 'Financial independence can mean stopping, scaling back, or simply gaining options.',
    choices: [
      { value: 'quit-completely', label: 'Leave paid work', description: 'Build toward fully funding your lifestyle from investments.' },
      { value: 'part-time', label: 'Work part-time', description: 'Use some earned income for expenses, benefits, or purpose.' },
      { value: 'coast', label: 'Shift into coast mode', description: 'Let investments grow while lower-stress work covers today.' },
      { value: 'flexible', label: 'Keep my options open', description: 'Create room to change how and when you work.' },
      { value: 'not-sure', label: 'Not sure yet', description: 'Explore paths with different relationships to work.' },
    ],
  },
  {
    id: 'timeline',
    type: 'choice',
    title: 'How soon would you like to reach financial independence?',
    subtitle: 'A range is enough. This shapes which strategy is most useful to explore first.',
    choices: [
      { value: 'within-5', label: 'Within 5 years', description: 'I have a near-term target.' },
      { value: '5-10', label: 'In 5–10 years', description: 'I want a focused medium-term plan.' },
      { value: '10-20', label: 'In 10–20 years', description: 'I have time to balance growth and flexibility.' },
      { value: '20-plus', label: 'More than 20 years', description: 'I can give compound growth a long runway.' },
      { value: 'not-sure', label: 'Not sure yet', description: 'I am still exploring what is realistic.' },
    ],
  },
  {
    id: 'primaryGoal',
    type: 'choice',
    title: 'What matters most in your FIRE plan?',
    subtitle: 'Pick the priority you would protect when tradeoffs appear.',
    choices: [
      { value: 'retire-early', label: 'Reach FI as soon as practical', description: 'Prioritize the timeline.' },
      { value: 'financial-security', label: 'Build financial security', description: 'Prioritize resilience and a balanced foundation.' },
      { value: 'maintain-lifestyle', label: 'Maintain my lifestyle', description: 'Prioritize spending capacity and margin.' },
      { value: 'flexibility', label: 'Create more flexibility', description: 'Prioritize options and work-life balance.' },
      { value: 'not-sure', label: 'Not sure yet', description: 'Compare several reasonable starting points.' },
    ],
  },
  {
    id: 'personalize',
    type: 'choice',
    title: 'Would you like to personalize the calculator you open?',
    subtitle: 'These optional details stay in your browser and do not change the core path recommendation.',
    choices: [
      { value: 'yes', label: 'Add my starting numbers', description: 'Answer five optional questions to prefill the calculator.' },
      { value: 'no', label: 'Show my matches now', description: 'Use calculator defaults and adjust them later.' },
    ],
  },
  {
    id: 'currentAge',
    type: 'number',
    title: 'What is your current age?',
    subtitle: 'Optional · Used only to prefill the calculator.',
    placeholder: '30',
    min: 18,
    max: 80,
  },
  {
    id: 'retirementAge',
    type: 'number',
    title: 'What age would you like to reach FI?',
    subtitle: 'Optional · A specific age helps personalize timeline calculations.',
    placeholder: '50',
    min: 19,
    max: 100,
  },
  {
    id: 'currentSavings',
    type: 'currency',
    title: 'How much do you currently have invested?',
    subtitle: 'Optional · Include retirement accounts and other invested assets.',
    placeholder: '100,000',
    min: 0,
  },
  {
    id: 'annualIncome',
    type: 'currency',
    title: 'What is your annual household income?',
    subtitle: 'Optional · Use income before taxes.',
    placeholder: '80,000',
    min: 0,
  },
  {
    id: 'annualExpenses',
    type: 'currency',
    title: 'What are your expected annual retirement expenses?',
    subtitle: 'Optional · This can fine-tune Lean and Fat FIRE matches.',
    placeholder: '50,000',
    min: 0,
  },
]

export default function FIREQuiz() {
  const navigate = useNavigate()
  const [step, setStep] = useState(0)
  const [answers, setAnswers] = useState<QuizAnswers>({})
  const [personalize, setPersonalize] = useState<boolean>()
  const [recommendation, setRecommendation] = useState<FireQuizRecommendation>()
  const [validationMessage, setValidationMessage] = useState('')
  const currentQuestion = questions[step]

  const currentValue = currentQuestion.id === 'personalize'
    ? personalize === undefined ? undefined : personalize ? 'yes' : 'no'
    : answers[currentQuestion.id as keyof QuizAnswers]

  const showRecommendation = (nextAnswers = answers) => {
    setRecommendation(recommendFirePaths(nextAnswers))
    window.scrollTo({ top: 0 })
  }

  const selectChoice = (questionId: ChoiceId, value: string) => {
    setValidationMessage('')
    if (questionId === 'personalize') {
      setPersonalize(value === 'yes')
      return
    }
    setAnswers(previous => ({ ...previous, [questionId]: value }))
  }

  const setNumericAnswer = (questionId: NumericId, value: string) => {
    setValidationMessage('')
    setAnswers(previous => ({
      ...previous,
      [questionId]: value === '' ? undefined : Number(value),
    }))
  }

  const validateNumericAnswer = (question: NumericQuestion) => {
    const value = answers[question.id]
    if (value === undefined) return true
    if (!Number.isFinite(value) || value < question.min || (question.max !== undefined && value > question.max)) {
      setValidationMessage(
        question.max === undefined
          ? `Enter an amount of ${question.min.toLocaleString()} or more, or choose “Not sure.”`
          : `Enter a value from ${question.min} to ${question.max}, or choose “Not sure.”`,
      )
      return false
    }
    if (question.id === 'retirementAge' && answers.currentAge !== undefined && value <= answers.currentAge) {
      setValidationMessage('Your target FI age must be after your current age, or choose “Not sure.”')
      return false
    }
    return true
  }

  const handleNext = () => {
    if (currentQuestion.type === 'choice' && currentValue === undefined) return
    if (currentQuestion.type !== 'choice' && !validateNumericAnswer(currentQuestion)) return
    if (currentQuestion.id === 'personalize' && personalize === false) {
      showRecommendation()
      return
    }
    if (step === questions.length - 1) {
      showRecommendation()
      return
    }
    setStep(previous => previous + 1)
    setValidationMessage('')
  }

  const skipNumericQuestion = () => {
    if (currentQuestion.type === 'choice') return
    const nextAnswers = { ...answers, [currentQuestion.id]: undefined }
    setAnswers(nextAnswers)
    setValidationMessage('')
    if (step === questions.length - 1) showRecommendation(nextAnswers)
    else setStep(previous => previous + 1)
  }

  const openCalculator = (match: FireQuizMatch) => {
    const params = new URLSearchParams()
    if (answers.currentAge !== undefined) params.set('age', answers.currentAge.toString())
    if (answers.retirementAge !== undefined) params.set('retire', answers.retirementAge.toString())
    if (answers.currentSavings !== undefined) params.set('savings', answers.currentSavings.toString())
    if (answers.annualIncome !== undefined) params.set('income', answers.annualIncome.toString())
    if (answers.annualExpenses !== undefined) params.set('expenses', answers.annualExpenses.toString())
    if (answers.annualIncome !== undefined && answers.annualExpenses !== undefined) {
      params.set('contrib', Math.max(0, answers.annualIncome - answers.annualExpenses).toString())
    }
    navigate(`${match.path}${params.size > 0 ? `?${params}` : ''}`)
  }

  const startOver = () => {
    setStep(0)
    setAnswers({})
    setPersonalize(undefined)
    setRecommendation(undefined)
    setValidationMessage('')
  }

  if (recommendation) {
    const primaryMetadata = getCalculatorByPath(recommendation.primary.path)
    return (
      <>
        <SEO {...calculatorSEO.quiz} />
        <main className="mx-auto max-w-3xl space-y-8">
          <header className="text-center">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">Your best FIRE starting point</h1>
            <p className="mx-auto mt-3 max-w-2xl text-gray-600 dark:text-gray-400">
              This is an educational match based on your priorities—not a prediction or financial advice.
            </p>
          </header>

          <Card className="border-2 border-fire-300 dark:border-fire-700">
            <CardContent className="p-6 sm:p-8">
              <div className="text-center">
                <span className="block text-5xl" aria-hidden="true">{primaryMetadata?.icon ?? '🎯'}</span>
                <h2 className="mt-4 text-3xl font-bold text-gray-900 dark:text-gray-100">{recommendation.primary.title}</h2>
                <p className="mt-3 text-lg font-medium text-fire-700 dark:text-fire-300">{recommendation.primary.reason}</p>
              </div>
              <p className="mx-auto mt-6 max-w-2xl text-gray-700 dark:text-gray-300">{recommendation.primary.description}</p>
              <ul className="mx-auto mt-5 max-w-2xl space-y-2">
                {recommendation.primary.benefits.map(benefit => (
                  <li key={benefit} className="flex gap-3 text-gray-700 dark:text-gray-300">
                    <span className="font-bold text-green-600 dark:text-green-400" aria-hidden="true">✓</span>
                    <span>{benefit}</span>
                  </li>
                ))}
              </ul>
              <button
                type="button"
                onClick={() => openCalculator(recommendation.primary)}
                className="mt-7 w-full rounded-lg bg-fire-600 px-6 py-3 font-semibold text-white transition-colors hover:bg-fire-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-fire-600"
              >
                Start with {recommendation.primary.title}
              </button>
            </CardContent>
          </Card>

          <section aria-labelledby="alternative-paths-heading">
            <h2 id="alternative-paths-heading" className="text-2xl font-bold text-gray-900 dark:text-gray-100">
              Two paths worth comparing
            </h2>
            <p className="mt-2 text-gray-600 dark:text-gray-400">
              FIRE paths overlap. These alternatives emphasize different tradeoffs in your answers.
            </p>
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              {recommendation.alternatives.map(match => {
                const metadata = getCalculatorByPath(match.path)
                return (
                  <article key={match.path} className="rounded-xl border border-gray-200 bg-white p-5 dark:border-gray-700 dark:bg-gray-900">
                    <div className="flex items-center gap-3">
                      <span className="text-2xl" aria-hidden="true">{metadata?.icon ?? '🎯'}</span>
                      <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100">{match.title}</h3>
                    </div>
                    <p className="mt-3 text-sm text-gray-600 dark:text-gray-400">{match.reason}</p>
                    <button
                      type="button"
                      onClick={() => openCalculator(match)}
                      className="mt-5 font-semibold text-fire-700 underline-offset-4 hover:underline dark:text-fire-300"
                    >
                      Explore {match.title}
                    </button>
                  </article>
                )
              })}
            </div>
          </section>

          <button
            type="button"
            onClick={startOver}
            className="mx-auto block rounded-lg border border-gray-300 px-5 py-3 font-medium text-gray-700 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            Retake the quiz
          </button>
        </main>
      </>
    )
  }

  const isOptionalStage = step >= 5
  const stageStep = isOptionalStage ? step - 4 : step + 1
  const stageTitle = isOptionalStage ? 'Optional detail' : 'Core question'
  const stageProgress = stageStep / 5

  return (
    <>
      <SEO {...calculatorSEO.quiz} />
      <main className="mx-auto max-w-2xl space-y-6">
        <header>
          <div className="flex items-center justify-between gap-4 text-sm">
            <span className="font-semibold text-gray-700 dark:text-gray-300">{stageTitle} {stageStep} of 5</span>
            <span className="text-gray-500 dark:text-gray-400">{isOptionalStage ? 'Skip anything you do not know' : 'About 2 minutes'}</span>
          </div>
          <div
            className="mt-2 h-2 overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700"
            role="progressbar"
            aria-label={`${stageTitle} progress`}
            aria-valuemin={0}
            aria-valuemax={5}
            aria-valuenow={stageStep}
          >
            <div className="h-full bg-fire-600 transition-[width] motion-reduce:transition-none" style={{ width: `${stageProgress * 100}%` }} />
          </div>
        </header>

        <Card>
          <CardContent className="p-6 sm:p-8">
            <h1 id="quiz-question" className="text-2xl font-bold text-gray-900 dark:text-gray-100">{currentQuestion.title}</h1>
            <p id="quiz-question-help" className="mt-2 text-gray-600 dark:text-gray-400">{currentQuestion.subtitle}</p>

            {currentQuestion.type === 'choice' ? (
              <fieldset className="mt-6 space-y-3">
                <legend className="sr-only">{currentQuestion.title}</legend>
                {currentQuestion.choices.map(choice => {
                  const selected = currentValue === choice.value
                  return (
                    <label
                      key={choice.value}
                      className={`block cursor-pointer rounded-lg border-2 p-4 transition-colors focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-fire-600 ${
                        selected
                          ? 'border-fire-500 bg-fire-50 dark:bg-fire-950/40'
                          : 'border-gray-200 hover:border-gray-400 dark:border-gray-700 dark:hover:border-gray-500'
                      }`}
                    >
                      <input
                        type="radio"
                        name={currentQuestion.id}
                        value={choice.value}
                        checked={selected}
                        onChange={() => selectChoice(currentQuestion.id, choice.value)}
                        className="sr-only"
                      />
                      <span className="block font-semibold text-gray-900 dark:text-gray-100">{choice.label}</span>
                      <span className="mt-1 block text-sm text-gray-600 dark:text-gray-400">{choice.description}</span>
                    </label>
                  )
                })}
              </fieldset>
            ) : (
              <div className="mt-6">
                <div className="relative">
                  {currentQuestion.type === 'currency' && (
                    <span className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-lg text-gray-500 dark:text-gray-400" aria-hidden="true">$</span>
                  )}
                  <input
                    id={`quiz-${currentQuestion.id}`}
                    type="number"
                    inputMode={currentQuestion.type === 'currency' ? 'decimal' : 'numeric'}
                    value={currentValue ?? ''}
                    onChange={event => setNumericAnswer(currentQuestion.id, event.target.value)}
                    placeholder={currentQuestion.placeholder}
                    min={currentQuestion.min}
                    max={currentQuestion.max}
                    aria-labelledby="quiz-question"
                    aria-describedby={`quiz-question-help${validationMessage ? ' quiz-validation' : ''}`}
                    aria-invalid={Boolean(validationMessage)}
                    className={`w-full rounded-lg border bg-white py-3 pr-4 text-lg text-gray-900 focus:outline-none focus:ring-2 focus:ring-fire-500 dark:bg-gray-800 dark:text-gray-100 ${
                      currentQuestion.type === 'currency' ? 'pl-8' : 'pl-4'
                    } ${validationMessage ? 'border-red-500' : 'border-gray-300 dark:border-gray-600'}`}
                  />
                </div>
                {validationMessage && (
                  <p id="quiz-validation" role="alert" className="mt-2 text-sm font-medium text-red-700 dark:text-red-300">
                    {validationMessage}
                  </p>
                )}
                <button
                  type="button"
                  onClick={skipNumericQuestion}
                  className="mt-3 font-medium text-gray-600 underline-offset-4 hover:underline dark:text-gray-300"
                >
                  I’m not sure
                </button>
              </div>
            )}
          </CardContent>
        </Card>

        <nav className="flex gap-3" aria-label="Quiz questions">
          <button
            type="button"
            onClick={() => {
              setStep(previous => Math.max(0, previous - 1))
              setValidationMessage('')
            }}
            disabled={step === 0}
            className="rounded-lg border border-gray-300 px-5 py-3 font-medium text-gray-700 hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-40 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            Previous
          </button>
          <button
            type="button"
            onClick={handleNext}
            disabled={currentQuestion.type === 'choice' && currentValue === undefined}
            className="flex-1 rounded-lg bg-fire-600 px-6 py-3 font-semibold text-white hover:bg-fire-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {currentQuestion.id === 'personalize' && personalize === false
              ? 'Show my matches'
              : step === questions.length - 1
                ? 'Show my matches'
                : 'Continue'}
          </button>
        </nav>
        <p className="text-center text-xs text-gray-500 dark:text-gray-400">
          Estimates only—not financial advice. Your answers stay in this browser.
        </p>
      </main>
    </>
  )
}
