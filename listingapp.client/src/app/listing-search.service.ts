import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Listing {
  id: string;
  source: string;
  address: string;
  city: string;
  state: string;
  zip: string;
  price: number;
  bedrooms: number;
  bathrooms: number;
  sqft: number;
  latitude: number;
  longitude: number;
  listedDate: string;
  status: string;
  description: string;
}

export interface ListingSearchItem {
  listing: Listing;
  relevanceScore: number;
}

export interface ListingSearchResponse {
  items: ListingSearchItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ListingSearchQuery {
  minPrice: number | null;
  maxPrice: number | null;
  minBedrooms: number | null;
  city: string;
  keyword: string;
  targetBudget: number | null;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ListingSearchService {
  constructor(private readonly http: HttpClient) {}

  search(query: ListingSearchQuery): Observable<ListingSearchResponse> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.minPrice !== null) params = params.set('minPrice', query.minPrice);
    if (query.maxPrice !== null) params = params.set('maxPrice', query.maxPrice);
    if (query.minBedrooms !== null) params = params.set('minBedrooms', query.minBedrooms);
    if (query.targetBudget !== null) params = params.set('targetBudget', query.targetBudget);
    if (query.city.trim()) params = params.set('city', query.city.trim());
    if (query.keyword.trim()) params = params.set('keyword', query.keyword.trim());

    return this.http.get<ListingSearchResponse>('/api/listings/search', { params });
  }
}
