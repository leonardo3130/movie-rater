import { z } from 'zod'

// The CSP forbids eval. Without jitless mode Zod probes `new Function` to decide whether to
// JIT-compile parsers, and the blocked probe triggers a CSP violation report.
// Imported first in main.tsx so it runs before any module builds a schema.
z.config({ jitless: true })
