import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ListingSearchItem, ListingSearchQuery, ListingSearchResponse, ListingSearchService } from './listing-search.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app-host.css'
})
export class App implements OnInit {
  filters = this.createDefaultFilters();
  results: ListingSearchResponse | null = null;
  isLoading = false;
  errorMessage = '';
  validationMessage = '';

  constructor(
    private readonly searchService: ListingSearchService,
    private readonly changeDetector: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadResults();
  }

  submitSearch(): void {
    this.validationMessage = this.validateFilters();
    if (this.validationMessage) return;

    this.loadResults(1);
  }

  resetSearch(): void {
    this.filters = this.createDefaultFilters();
    this.validationMessage = '';
    this.loadResults(1);
  }

  changePage(page: number): void {
    if (this.results && page >= 1 && page <= this.results.totalPages) {
      this.loadResults(page);
    }
  }

  trackBySourceAndId(_index: number, item: ListingSearchItem): string {
    return `${item.listing.source}:${item.listing.id}`;
  }

  private createDefaultFilters(): ListingSearchQuery {
    return {
      minPrice: null,
      maxPrice: null,
      minBedrooms: null,
      city: '',
      keyword: '',
      targetBudget: 500000,
      page: 1,
      pageSize: 6
    };
  }

  private loadResults(page = this.filters.page): void {
    this.filters.page = page;
    this.isLoading = true;
    this.errorMessage = '';

    this.searchService.search(this.filters).subscribe({
      next: (response) => {
        this.results = response;
        this.isLoading = false;
        this.changeDetector.markForCheck();
      },
      error: (error: unknown) => {
        this.errorMessage = this.getErrorMessage(error);
        this.results = null;
        this.isLoading = false;
        this.changeDetector.markForCheck();
      }
    });
  }

  private validateFilters(): string {
    if (this.filters.minPrice !== null && (!Number.isFinite(this.filters.minPrice) || this.filters.minPrice < 0)) {
      return 'Minimum price must be zero or greater.';
    }
    if (this.filters.maxPrice !== null && (!Number.isFinite(this.filters.maxPrice) || this.filters.maxPrice < 0)) {
      return 'Maximum price must be zero or greater.';
    }
    if (this.filters.minPrice !== null && this.filters.maxPrice !== null && this.filters.minPrice > this.filters.maxPrice) {
      return 'Minimum price cannot exceed maximum price.';
    }
    if (this.filters.minBedrooms !== null && (!Number.isInteger(this.filters.minBedrooms) || this.filters.minBedrooms < 0)) {
      return 'Minimum bedrooms must be a whole number of zero or greater.';
    }
    if (this.filters.targetBudget !== null && (!Number.isFinite(this.filters.targetBudget) || this.filters.targetBudget <= 0)) {
      return 'Target budget must be greater than zero, or leave it blank.';
    }
    if (!Number.isInteger(this.filters.pageSize) || this.filters.pageSize < 1 || this.filters.pageSize > 100) {
      return 'Results per page must be between 1 and 100.';
    }
    return '';
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const payload = error.error as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
      if (payload?.errors) return Object.values(payload.errors).flat().join(' ');
      return payload?.detail ?? payload?.title ?? 'The listings could not be loaded. Please try again.';
    }

    return 'The listings could not be loaded. Please try again.';
  }
}
