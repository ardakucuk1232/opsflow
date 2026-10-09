const TURKISH_LETTERS: Record<string, string> = {
  ç: "c",
  ğ: "g",
  ı: "i",
  i: "i",
  ö: "o",
  ş: "s",
  ü: "u",
};

function toAsciiWords(name: string): string[] {
  return name
    .toLocaleLowerCase("tr-TR")
    .replace(/[çğıiöşü]/g, (letter) => TURKISH_LETTERS[letter] ?? letter)
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, " ")
    .trim()
    .split(" ")
    .filter(Boolean);
}

export function suggestProjectKey(name: string): string {
  const words = toAsciiWords(name);

  if (words.length === 0) {
    return "";
  }

  const firstLetterWords = words.filter((word) => /^[a-z]/.test(word));
  const source = firstLetterWords.length > 0 ? firstLetterWords : words;

  const key = source.length === 1
    ? source[0].slice(0, 4)
    : source.map((word) => word[0]).join("").slice(0, 6);

  return /^[a-z]/.test(key) ? key.toUpperCase() : "";
}
