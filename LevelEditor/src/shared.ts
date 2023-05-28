export function fileName(filepath: string) {
  if (!filepath) return filepath;
  return filepath.split(/[\\/]/).pop();
}
