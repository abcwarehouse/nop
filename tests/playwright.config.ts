import { PlaywrightTestConfig } from '@playwright/test';

const config: PlaywrightTestConfig = {
    use: {
        baseURL: "https://www.abcwarehouse.com"
    }
};

export default config;