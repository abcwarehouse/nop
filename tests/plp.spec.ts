import { test, expect } from '@playwright/test';

test('can open product listing page', async ({ page }) => {
    await page.goto('/front-load-washers-2');
    await page.waitForSelector('.product-grid');
    const items = page.$$('.item-box');
    expect((await items).length).toBeCloseTo(20);
});