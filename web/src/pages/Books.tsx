import { Lightbulb } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/ui'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

interface Book {
  title: string
  author: string
  description: string
  amazonUrl: string
  imageUrl: string
}

const books: Book[] = [
  {
    title: "I Will Teach You to Be Rich",
    author: "Ramit Sethi",
    description: "A practical, no-BS 6-week program for 20-to-35-year-olds that covers banking, saving, budgeting, and investing. Perfect for beginners who want to automate their finances.",
    amazonUrl: "https://amzn.to/3N1SrtP",
    imageUrl: "https://m.media-amazon.com/images/I/81c9SSbG3OL._SL1500_.jpg"
  },
  {
    title: "Money for Couples",
    author: "Ramit Sethi",
    description: "From the author of I Will Teach You to Be Rich, this book helps couples navigate the often-tricky world of combining finances, from joint accounts to big purchases.",
    amazonUrl: "https://amzn.to/4pQ81Hn",
    imageUrl: "https://m.media-amazon.com/images/I/81G3ygJ-jOL._SL1500_.jpg"
  },
  {
    title: "The Psychology of Money",
    author: "Morgan Housel",
    description: "Timeless lessons on wealth, greed, and happiness. This book explores how our personal history and emotions shape our financial decisions in ways we often don't realize.",
    amazonUrl: "https://amzn.to/3Y74Jn9",
    imageUrl: "https://m.media-amazon.com/images/I/81Dky+tD+pL._SY522_.jpg"
  },
  {
    title: "The Bogleheads' Guide to Investing",
    author: "Taylor Larimore, Mel Lindauer, Michael LeBoeuf",
    description: "The definitive guide to index fund investing, based on the philosophy of Vanguard founder John Bogle. Learn the simple, proven approach to building wealth.",
    amazonUrl: "https://amzn.to/3MXrOWU",
    imageUrl: "https://m.media-amazon.com/images/I/611brjp7lgL._SL1200_.jpg"
  },
  {
    title: "We Need to Talk: A Memoir About Wealth",
    author: "Jennifer Risher",
    description: "A candid memoir about navigating sudden wealth after Microsoft stock options. An honest look at the emotional and social complexities of money.",
    amazonUrl: "https://amzn.to/3Y74Ij5",
    imageUrl: "https://m.media-amazon.com/images/I/81KH2bo+b0L._SL1500_.jpg"
  },
  {
    title: "Die with Zero",
    author: "Bill Perkins",
    description: "A bold counterpoint to traditional retirement advice. This book argues for optimizing life experiences over accumulating wealth you'll never spend.",
    amazonUrl: "https://amzn.to/3LgBMlK",
    imageUrl: "https://m.media-amazon.com/images/I/61+4EHZ4faL._SL1500_.jpg"
  },
  {
    title: "The Little Book of Common Sense Investing",
    author: "John C. Bogle",
    description: "John Bogle's classic on why low-cost index funds are the smartest way for most investors to build wealth. The foundation of passive investing.",
    amazonUrl: "https://amzn.to/4pdtMQq",
    imageUrl: "https://m.media-amazon.com/images/I/81vPxCvGMcL._SL1500_.jpg"
  },
]

export default function Books() {
  return (
    <>
      <SEO {...calculatorSEO.books} />
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-content sm:text-3xl">Recommended FIRE Books
          </h1>
          <p className="text-content-muted mt-1">
            Essential reading for your financial independence journey.
          </p>
        </div>

        {/* Info Banner */}
        <div className="bg-warning-subtle border border-warning/30 rounded-container p-4">
          <div className="flex gap-3">
            <Lightbulb className="h-5 w-5 shrink-0 text-warning" aria-hidden="true" />
            <div>
            <h3 className="font-semibold text-content">Knowledge is Power</h3>
            <p className="text-sm text-warning mt-1">
              These books have helped millions achieve financial independence. Whether you're just starting out 
              or optimizing your FIRE strategy, there's something here for everyone.
            </p>
          </div>
        </div>
      </div>

      {/* Books Grid */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {books.map((book) => (
          <a
            key={book.title}
            href={book.amazonUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="group"
          >
            <Card className="h-full transition-colors duration-200 hover:border-border-strong motion-reduce:transition-none">
              <CardContent className="p-4">
                <div className="aspect-[2/3] mb-4 overflow-hidden rounded-control bg-surface-sunken">
                  <img
                    src={book.imageUrl}
                    alt={`${book.title} book cover`}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                    loading="lazy"
                  />
                </div>
                <h3 className="font-semibold text-content group-hover:text-accent transition-colors line-clamp-2">
                  {book.title}
                </h3>
                <p className="text-sm text-content-subtle mt-1">
                  {book.author}
                </p>
                <p className="text-sm text-content-muted mt-2 line-clamp-3">
                  {book.description}
                </p>
                <div className="mt-4 flex items-center gap-2 text-accent text-sm font-medium">
                  <span>View on Amazon</span>
                  <svg className="w-4 h-4 group-hover:translate-x-1 transition-transform" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 5l7 7m0 0l-7 7m7-7H3" />
                  </svg>
                </div>
              </CardContent>
            </Card>
          </a>
        ))}
      </div>

      {/* Disclaimer */}
      <Card className="bg-surface-sunken border-border-subtle">
        <CardHeader>
          <h2 className="text-sm font-semibold text-content-muted">Affiliate Disclosure</h2>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-content-muted">
            The links above are Amazon affiliate links. If you purchase through these links, we may earn a small 
            commission at no additional cost to you. This helps support the development of this free calculator. 
            We only recommend books we genuinely believe will help you on your FIRE journey.
          </p>
        </CardContent>
      </Card>
    </div>
    </>
  )
}
