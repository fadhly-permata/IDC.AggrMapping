---
name: Generate Documentations
description: Create detailed documentations
invokable: true
---

- Adjust the documentation format to the programming language used in the file
- Use English with formal tone for XML documentation
- Include comprehensive sections: Summary, Sample Code, Parameters, Returns, Exceptions
- Generate documentation for private/internal methods too
- Use DocFX-compatible XML with `<code>` tags
- Limit lines to 100 characters maximum
- Use DocFX alerts: `> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`, `> [!CAUTION]`, `> [!WARNING]`
- Do not make unnecessary changes to existing code