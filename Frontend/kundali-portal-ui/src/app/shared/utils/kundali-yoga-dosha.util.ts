function normalizeImpactAreas(rawImpactAreas: any): any[] {
  if (!Array.isArray(rawImpactAreas)) {
    return [];
  }

  return rawImpactAreas
    .map((area: any) => {
      const category = area?.category ?? area?.Category;
      const statements = area?.statements ?? area?.Statements ?? [];

      if (!category || !Array.isArray(statements) || !statements.length) {
        return null;
      }

      return {
        category,
        statements
      };
    })
    .filter(Boolean);
}

function normalizeActivationList(rawList: any): any[] {
  if (!Array.isArray(rawList)) {
    return [];
  }

  return rawList.filter(Boolean);
}

function normalizeActivationPeriods(rawActivationPeriods: any): any {
  if (!rawActivationPeriods) {
    return null;
  }

  const source = rawActivationPeriods;

  return {
    currentlyActive: normalizeActivationList(source.currentlyActive ?? source.CurrentlyActive),
    upcoming: normalizeActivationList(source.upcoming ?? source.Upcoming),
    past: normalizeActivationList(source.past ?? source.Past)
  };
}

export function extractYogas(response: any): any[] {
  const rawYogas = response?.yogas ?? response?.summary?.yogas ?? response?.summary?.yoga ?? response?.yoga;

  if (Array.isArray(rawYogas)) {
    return rawYogas
      .map((item: any) => {
        if (!item) {
          return null;
        }

        if (typeof item === 'string') {
          const trimmed = item.trim();
          return trimmed ? { name: trimmed } : null;
        }

        const name = item.name || item.title || item.label || item.yogaName;

        if (!name) {
          return null;
        }

        return {
          name: String(name),
          category: item.category,
          strength: item.strength,
          reason: item.reason,
          sanskritName: item.sanskritName,
          description: item.description,
          impact: item.impact,
          involvedPlanets: item.involvedPlanets ?? item.planets ?? [],
          involvedHouses: item.involvedHouses ?? item.houses ?? [],
          impactAreas: normalizeImpactAreas(item.impactAreas ?? item.ImpactAreas ?? item.effects ?? item.Effects ?? []),
          activationPeriods: normalizeActivationPeriods(item.activationPeriods ?? item.ActivationPeriods)
        };
      })
      .filter(Boolean);
  }

  if (typeof rawYogas === 'string') {
    return rawYogas
      .split(',')
      .map((item: string) => item.trim())
      .filter(Boolean)
      .map((name: string) => ({ name }));
  }

  return [];
}

export function extractDoshas(response: any): any[] {
  const rawDoshas = response?.doshas ?? response?.summary?.doshas ?? response?.summary?.dosha ?? response?.dosha;

  if (Array.isArray(rawDoshas)) {
    return rawDoshas
      .map((item: any) => {
        if (!item) {
          return null;
        }

        if (typeof item === 'string') {
          const trimmed = item.trim();
          return trimmed ? { name: trimmed } : null;
        }

        const name = item.name || item.Name || item.title || item.label || item.doshaName || item.DoshaName;

        if (!name) {
          return null;
        }

        return {
          name: String(name),
          category: item.category ?? item.Category,
          severity: item.severity ?? item.Severity ?? item.strength ?? item.Strength ?? item.status ?? item.Status,
          reason: item.reason ?? item.Reason,
          description: item.description ?? item.Description,
          involvedPlanets: item.involvedPlanets ?? item.InvolvedPlanets ?? item.planets ?? item.Planets ?? [],
          involvedHouses: item.involvedHouses ?? item.InvolvedHouses ?? item.houses ?? item.Houses ?? [],
          impactAreas: normalizeImpactAreas(item.impactAreas ?? item.ImpactAreas ?? item.effects ?? item.Effects ?? []),
          remedies: item.remedies ?? item.Remedies ?? [],
          activationPeriods: normalizeActivationPeriods(item.activationPeriods ?? item.ActivationPeriods)
        };
      })
      .filter(Boolean);
  }

  if (typeof rawDoshas === 'string') {
    return rawDoshas
      .split(',')
      .map((item: string) => item.trim())
      .filter(Boolean)
      .map((name: string) => ({ name }));
  }

  return [];
}
