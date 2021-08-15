import { test, expect } from '@playwright/test';

test('can open homepage', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('.home-page-category-grid');
    const categoryGrids = page.$$('.category-item');
    expect((await categoryGrids).length).toBeCloseTo(8);
});