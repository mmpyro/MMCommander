# Filter language

<video autoplay loop muted playsinline poster="../images/filter.png" aria-label="Typing ext -> (png, pdf) into the filter box">
  <source src="../images/filter.mp4" type="video/mp4">
</video>

Type an expression into the filter box above the list. The list updates 1 s after the last key press.

| Expression | Meaning |
|---|---|
| `ext == pdf` / `ext != pdf` | Extension is / is not `pdf` |
| `ext -> (pdf, txt)` / `ext <> (pdf, txt)` | Extension is in / not in the set |
| `name == report` | Name without extension equals |
| `name like rep` | Name contains |
| `type is dir` / `type is file` | Only folders / only files |
| `a & b`, `a \| b` | And / or, applied left to right |

- Example: `ext -> (xml, pdf) & name like il`.
- Write the keywords in lowercase. The filter ignores case when it compares values.
- Help → Filter syntax shows the same table in the app.
