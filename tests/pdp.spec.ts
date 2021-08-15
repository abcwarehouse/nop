import { test, expect } from '@playwright/test';

test('can open product detail page', async ({ page }) => {
    await page.goto('/samsung-wf45r6100ac-front-load-washer');
    await page.waitForSelector('.product-name > h1');
    const textItem = await page.$('.product-name > h1');
    const text = await page.evaluate(el => el.innerText, textItem);
    expect(text).toBe('SAMSUNG WF45R6100AC');
});