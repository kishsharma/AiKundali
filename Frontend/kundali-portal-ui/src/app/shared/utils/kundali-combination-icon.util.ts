import { AstrologyIconName } from '../components/astrology-icon.component';

type KundaliCombinationKind = 'yoga' | 'dosha';

export function getKundaliCombinationIcon(item: any, kind: KundaliCombinationKind): AstrologyIconName {
  const name = String(item?.name ?? '').toLowerCase().replace(/[^a-z]/g, '');
  const namedIcons: Array<[RegExp, AstrologyIconName]> = kind === 'yoga'
    ? [
        [/gajakesari|kesari/, 'jupiter'], [/budhaditya|adityabudh/, 'sun'], [/chandra|moon/, 'moon'],
        [/mangal|kujadosha/, 'mars'], [/shukra|venus/, 'venus'], [/shani|saturn/, 'saturn'],
        [/rahu/, 'rahu'], [/ketu/, 'ketu'], [/surya|aditya/, 'sun'], [/budh|mercury/, 'mercury'], [/guru|jupiter/, 'jupiter']
      ]
    : [
        [/mangal|kuja/, 'mars'], [/kaalsarp|sarpa|rahu/, 'rahu'], [/pitru/, 'ketu'],
        [/chandra/, 'moon'], [/shani/, 'saturn'], [/grahan/, 'sun'], [/guru/, 'jupiter']
      ];

  const namedIcon = namedIcons.find(([pattern]) => pattern.test(name))?.[1];
  if (namedIcon) {
    return namedIcon;
  }

  const rawPlanets = item?.involvedPlanets ?? item?.planets;
  const planet = Array.isArray(rawPlanets) ? rawPlanets[0] : rawPlanets;
  const planetName = String(planet?.name ?? planet?.planet ?? planet ?? '').toLowerCase().replace(/[^a-z]/g, '');
  const planetIcons: Record<string, AstrologyIconName> = {
    sun: 'sun', surya: 'sun', moon: 'moon', chandra: 'moon', mars: 'mars', mangal: 'mars', kuja: 'mars',
    mercury: 'mercury', budh: 'mercury', jupiter: 'jupiter', guru: 'jupiter', venus: 'venus', shukra: 'venus',
    saturn: 'saturn', shani: 'saturn', rahu: 'rahu', ketu: 'ketu'
  };
  return planetIcons[planetName] ?? (kind === 'yoga' ? 'yogas' : 'doshas');
}
