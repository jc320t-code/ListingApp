import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { App } from './app';
import { ListingSearchResponse } from './listing-search.service';

describe('App', () => {
  let component: App;
  let fixture: ComponentFixture<App>;
  let httpMock: HttpTestingController;

  const response = (overrides: Partial<ListingSearchResponse> = {}): ListingSearchResponse => ({
    items: [{
      listing: {
        id: 'A1', source: 'MLS_A', address: '123 Main St', city: 'Springfield', state: 'VA', zip: '22150',
        price: 450000, bedrooms: 2, bathrooms: 1.5, sqft: 980, latitude: 38.7893, longitude: -77.1873,
        listedDate: '2026-08-29', status: 'active', description: 'Bright condo near shops.'
      },
      relevanceScore: 96.4
    }],
    page: 1,
    pageSize: 6,
    totalCount: 1,
    totalPages: 1,
    ...overrides
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [App],
      imports: [HttpClientTestingModule, FormsModule]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(App);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads and renders ranked listing results', () => {
    fixture.detectChanges(false);

    const req = httpMock.expectOne(request => request.url === '/api/listings/search');
    expect(req.request.method).toEqual('GET');
    expect(req.request.params.get('targetBudget')).toBe('500000');
    req.flush(response());
    fixture.detectChanges(false);

    expect(component).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('123 Main St');
    expect(fixture.nativeElement.textContent).toContain('96.4');
    expect(fixture.nativeElement.textContent).toContain('Springfield');
  });

  it('submits filters and resets to the first page', () => {
    fixture.detectChanges(false);
    httpMock.expectOne(request => request.url === '/api/listings/search').flush(response({ totalCount: 8, totalPages: 2 }));

    component.filters.city = 'Fairfax';
    component.filters.keyword = 'cozy';
    component.filters.page = 2;
    component.submitSearch();

    const req = httpMock.expectOne(request => request.url === '/api/listings/search' && request.params.get('city') === 'Fairfax');
    expect(req.request.params.get('keyword')).toBe('cozy');
    expect(req.request.params.get('page')).toBe('1');
    req.flush(response({ page: 1 }));
  });

  it('requests the next page and displays updated pagination', () => {
    fixture.detectChanges(false);
    httpMock.expectOne(request => request.url === '/api/listings/search').flush(response({ totalCount: 8, totalPages: 2 }));

    component.changePage(2);

    const req = httpMock.expectOne(request => request.url === '/api/listings/search' && request.params.get('page') === '2');
    req.flush(response({ page: 2, totalCount: 8, totalPages: 2 }));
    fixture.detectChanges(false);

    expect(fixture.nativeElement.textContent).toContain('Page 2 of 2');
  });

  it('shows an empty state when no listings match', () => {
    fixture.detectChanges(false);
    httpMock.expectOne(request => request.url === '/api/listings/search').flush(response({ items: [], totalCount: 0, totalPages: 0 }));
    fixture.detectChanges(false);

    expect(fixture.nativeElement.textContent).toContain('No homes match those filters.');
  });

  it('shows a useful error state when the API fails', () => {
    fixture.detectChanges(false);
    httpMock.expectOne(request => request.url === '/api/listings/search').flush({ title: 'Service unavailable' }, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges(false);

    expect(fixture.nativeElement.textContent).toContain("We couldn't load listings.");
    expect(fixture.nativeElement.textContent).toContain('Service unavailable');
  });

  it('blocks invalid price ranges before sending another request', () => {
    fixture.detectChanges(false);
    httpMock.expectOne(request => request.url === '/api/listings/search').flush(response());

    component.filters.minPrice = 500000;
    component.filters.maxPrice = 400000;
    component.submitSearch();
    fixture.detectChanges(false);

    expect(component.validationMessage).toBe('Minimum price cannot exceed maximum price.');
    httpMock.expectNone(request => request.url === '/api/listings/search');
  });
});
