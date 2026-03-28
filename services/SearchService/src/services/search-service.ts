import { Client } from '@elastic/elasticsearch';
import { createLogger } from '@hospital/shared';

const logger = createLogger('search-service');

interface SearchResult {
  items: Array<{ id: string; score: number; [key: string]: unknown }>;
  total: number;
  page: number;
  pageSize: number;
}

export class SearchService {
  constructor(private readonly client: Client) {}

  async search(
    query: string,
    type: 'patient' | 'drug' = 'patient',
    page = 1,
    pageSize = 50,
  ): Promise<SearchResult> {
    const index = type === 'patient' ? 'hospital-patients' : 'hospital-drugs';
    const fields = type === 'patient' ? ['firstName', 'lastName', 'email'] : ['name', 'code'];
    const from = (page - 1) * pageSize;

    logger.info('Executing search', { query, type, page });

    const result = await this.client.search({
      index,
      from,
      size: pageSize,
      query: {
        multi_match: { query, fields },
      },
    });

    const total =
      typeof result.hits.total === 'number'
        ? result.hits.total
        : (result.hits.total?.value ?? 0);

    return {
      items: result.hits.hits.map(h => ({
        id: h._id ?? '',
        score: h._score ?? 0,
        ...(h._source as object),
      })),
      total,
      page,
      pageSize,
    };
  }
}
