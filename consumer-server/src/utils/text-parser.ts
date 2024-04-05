export function parseMultilineText(text: string): string {
  return text.replace(/\n|\r\n/g, ' ').replace(/\t/g, ' | ')
}
