# 🔥 FIRE Calculators

A **private**, **offline-first** FIRE (Financial Independence, Retire Early) calculator. Calculate your path to financial freedom with 6 different calculator types—all running 100% in your browser.

![FIRE Calculators](https://img.shields.io/badge/FIRE-Calculator-orange)
![Privacy First](https://img.shields.io/badge/Privacy-First-green)
![Offline Ready](https://img.shields.io/badge/Offline-Ready-blue)

## ✨ Features

### 🔒 Privacy First
- **No cookies** - We don't use any cookies
- **No financial data storage** - Calculator data only in URLs, never stored
- **UI preferences only** - Only theme and layout preferences stored locally
- **No analytics** - Zero tracking scripts
- **No servers** - All calculations run client-side
- **URL-based state** - Share your calculations via URL (your choice!)

### 📱 Works Offline
After first load, the app works completely offline. Install it as a PWA on your device for the best experience.

### 🧮 9 Calculators

| Calculator | Description |
|------------|-------------|
| 🎯 **Standard FIRE** | Classic 25x expenses rule - calculate your "magic number" |
| ⛵ **Coast FIRE** | Find how much you need so compound growth does the rest |
| 🌿 **Lean FIRE** | Achieve FI faster with a minimalist lifestyle (≤$40k/year) |
| 💎 **Fat FIRE** | Retire in style without compromising ($100k+/year) |
| ☕ **Barista FIRE** | Blend part-time work with portfolio income |
| 📊 **Withdrawal Rate** | Test portfolio longevity at different withdrawal rates |
| 🧮 **Savings Rate** | Calculate how your savings rate impacts time to FIRE |
| 🔄 **Reverse FIRE** | Work backwards - set target age, find required savings |
| 🏥 **Healthcare Gap** | Estimate costs between early retirement and Medicare |

### 🎨 Beautiful Design
- Clean, modern interface
- Dark mode with system preference detection
- Fully responsive (mobile, tablet, desktop)
- Interactive charts powered by Recharts
- Progress bars showing your journey to FIRE
- Quick presets for common scenarios

## 🚀 Getting Started

### Visit the Live Site
[https://myfirenumber.com](https://myfirenumber.com)

### Run Locally

```bash
# Clone the repository
git clone https://github.com/jamesmontemagno/app-fire-calculator.git
cd app-fire-calculator

# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build
```

## 🛠️ Tech Stack

- **React 19** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool
- **Tailwind CSS v4** - Styling
- **React Router v7** - Routing & URL state
- **Recharts** - Charts
- **vite-plugin-pwa** - Offline support

## 📐 FIRE Calculations

### Standard FIRE Number
```
FIRE Number = Annual Expenses / Withdrawal Rate
```
Example: $48,000 / 0.04 = **$1,200,000**

### Coast FIRE Number
```
Coast Number = FIRE Number / (1 + Real Return)^Years
```

### Barista FIRE Number
```
Barista Number = (Annual Expenses - Part-Time Income) / Withdrawal Rate
```

## 🔗 URL State

All calculator inputs are stored in the URL. This means:
- ✅ Bookmark your calculations
- ✅ Share links with specific values
- ✅ Browser back/forward works
- ✅ No data stored anywhere

Example URL:
```
/standard?age=30&retire=55&savings=100000&contrib=24000&expenses=48000
```

## 📝 License

MIT License - Feel free to use, modify, and distribute.

## 🙏 Acknowledgments

- The FIRE community for spreading financial literacy
- Trinity Study for the 4% rule research
- All open-source contributors

---

**Built with ❤️ for the FIRE community**

*Remember: This calculator provides estimates for educational purposes. Consult a financial advisor for personalized advice.*
