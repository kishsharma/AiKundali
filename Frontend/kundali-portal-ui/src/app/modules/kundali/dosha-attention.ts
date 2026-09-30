export type DoshaAttentionTag = 'strong' | 'period' | 'lifetime';

export interface DoshaAttentionLine {
  key: string;
  params: Record<string, string>;
}

export interface DoshaAttentionEntry {
  index: number;
  name: string;
  kind: 'strong' | 'period' | 'lifetime' | 'quiet';
  tags: DoshaAttentionTag[];
  score: number;
  remedies: string[];
  lines: DoshaAttentionLine[];
}

export interface DoshaAttentionReport {
  focus: DoshaAttentionEntry | null;
  also: DoshaAttentionEntry[];
  quiet: DoshaAttentionEntry[];
  usedTiming: boolean;
}

export interface DoshaAttentionFormatters {
  formatPeriod?: (activation: any) => string;
  planet?: (name: string) => string;
}

export function rankDoshaAttention(doshas: any[] | null | undefined, formatters: DoshaAttentionFormatters = {}): DoshaAttentionReport {
  return rankAttention(doshas, formatters, 'kundali.doshaAttention', 'severity');
}

export function rankYogaAttention(yogas: any[] | null | undefined, formatters: DoshaAttentionFormatters = {}): DoshaAttentionReport {
  return rankAttention(yogas, formatters, 'kundali.yogaAttention', 'strength');
}

function rankAttention(
  items: any[] | null | undefined,
  formatters: DoshaAttentionFormatters,
  prefix: string,
  strengthKey: 'severity' | 'strength'
): DoshaAttentionReport {
  const list = Array.isArray(items) ? items : [];
  const ranked = list
    .map((item, index) => scoreItem(item, index, formatters, prefix, strengthKey))
    .filter((entry): entry is DoshaAttentionEntry => !!entry);

  const attention = ranked
    .filter((entry) => entry.kind !== 'quiet')
    .sort(compareAttention);
  const quiet = ranked
    .filter((entry) => entry.kind === 'quiet')
    .sort(compareAttention);

  return {
    focus: attention[0] ?? null,
    also: attention.slice(1, 3),
    quiet,
    usedTiming: list.some((item) => hasTiming(item))
  };
}

function scoreItem(
  item: any,
  index: number,
  formatters: DoshaAttentionFormatters,
  prefix: string,
  strengthKey: 'severity' | 'strength'
): DoshaAttentionEntry | null {
  const name = String(item?.name ?? item?.Name ?? '').trim();
  if (!name) {
    return null;
  }

  const severity = readImpact(item, strengthKey);
  const current = firstActivation(item, 'currentlyActive', 'CurrentlyActive');
  const upcoming = firstActivation(item, 'upcoming', 'Upcoming');
  const currentLevel = readLevel(current);
  const upcomingLevel = readLevel(upcoming);
  const tags: DoshaAttentionTag[] = [];
  const lines: DoshaAttentionLine[] = [];
  let score = severity === 'high' ? 36 : severity === 'medium' ? 18 : 6;
  let kind: DoshaAttentionEntry['kind'] = 'quiet';

  if (severity === 'high') {
    tags.push('lifetime');
    lines.push({ key: `${prefix}.lifetimeHigh`, params: {} });
  } else if (severity === 'medium' && (currentLevel === 'strong' || currentLevel === 'active')) {
    lines.push({ key: `${prefix}.lifetimeMedium`, params: {} });
  }

  if (currentLevel === 'strong') {
    score += 48;
    kind = 'strong';
    tags.unshift('strong');
    lines.unshift(timingLine(`${prefix}.strongNow`, current, formatters));
  } else if (currentLevel === 'active') {
    score += 28;
    kind = 'period';
    tags.unshift('period');
    lines.unshift(timingLine(`${prefix}.activeNow`, current, formatters));
  } else if (currentLevel === 'mild' && severity === 'high') {
    score += 10;
    kind = 'period';
    tags.unshift('period');
    lines.unshift(timingLine(`${prefix}.mildNow`, current, formatters));
  } else if (severity === 'high') {
    kind = 'lifetime';
    if (upcomingLevel === 'strong' || upcomingLevel === 'active') {
      lines.push(timingLine(`${prefix}.upcoming`, upcoming, formatters));
    }
  }

  if (kind === 'quiet') {
    lines.push({ key: `${prefix}.quietLine`, params: {} });
  }

  return {
    index,
    name,
    kind,
    tags,
    score,
    remedies: readRemedies(item).slice(0, 2),
    lines
  };
}

function timingLine(key: string, activation: any, formatters: DoshaAttentionFormatters): DoshaAttentionLine {
  const maha = readPlanet(activation, 'mahadashaPlanet', 'MahadashaPlanet');
  const antar = readPlanet(activation, 'antardashaPlanet', 'AntardashaPlanet');
  const planet = formatters.planet ?? ((value: string) => value);

  return {
    key,
    params: {
      maha: maha ? planet(maha) : '',
      antar: antar ? planet(antar) : '',
      period: formatters.formatPeriod?.(activation) || ''
    }
  };
}

function compareAttention(left: DoshaAttentionEntry, right: DoshaAttentionEntry): number {
  return right.score - left.score || left.name.localeCompare(right.name);
}

function readImpact(item: any, key: 'severity' | 'strength'): 'high' | 'medium' | 'low' {
  const pascal = key === 'severity' ? 'Severity' : 'Strength';
  const value = String(item?.[key] ?? item?.[pascal] ?? '').toLowerCase();
  if (value.includes('high') || value.includes('strong')) {
    return 'high';
  }
  if (value.includes('medium') || value.includes('moderate')) {
    return 'medium';
  }
  return 'low';
}

function readLevel(activation: any): 'strong' | 'active' | 'mild' | '' {
  const value = String(activation?.activationLevel ?? activation?.ActivationLevel ?? '').toLowerCase();
  if (value === 'strong' || value === 'active' || value === 'mild') {
    return value;
  }
  return '';
}

function firstActivation(item: any, camel: string, pascal: string): any | null {
  const periods = item?.activationPeriods ?? item?.ActivationPeriods;
  const list = periods?.[camel] ?? periods?.[pascal];
  return Array.isArray(list) ? list.find(Boolean) ?? null : null;
}

function hasTiming(item: any): boolean {
  return !!(firstActivation(item, 'currentlyActive', 'CurrentlyActive') || firstActivation(item, 'upcoming', 'Upcoming'));
}

function readPlanet(activation: any, camel: string, pascal: string): string {
  return String(activation?.[camel] ?? activation?.[pascal] ?? '').trim();
}

function readRemedies(item: any): string[] {
  const remedies = item?.remedies ?? item?.Remedies;
  if (!Array.isArray(remedies)) {
    return [];
  }

  return remedies.map((remedy) => String(remedy ?? '').trim()).filter(Boolean);
}
