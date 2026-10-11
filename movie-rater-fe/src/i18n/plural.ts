export interface PluralMessage {
  zero?: string
  one: string
  two?: string
  few?: string
  many?: string
  other: string
}

const rulesCache = new Map<string, Intl.PluralRules>()

export function getPluralRule(locale: string): Intl.PluralRules {
  let rule = rulesCache.get(locale)
  if (!rule) {
    rule = new Intl.PluralRules(locale)
    rulesCache.set(locale, rule)
  }
  return rule
}

export function selectPluralForm(locale: string, count: number): Intl.LDMLPluralRule {
  return getPluralRule(locale).select(count)
}