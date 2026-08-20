const emailPattern = /^[^\s@]+@[^\s@]+\.[A-Za-z]{2,}$/;

export function isValidEmailAddress(value: string): boolean {
  const email = value.trim();
  return email.length <= 254 && emailPattern.test(email);
}