import { MetadataRoute } from 'next';
import shopProductApi from '@/api/shopProductApi';
import { PagedResult } from '@/types/common';
import { ShopProductCard, ShopCategoryTree, ShopCategoryItem, ShopPromotionBadge } from '@/types/product';

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
    const baseUrl = process.env.NEXT_PUBLIC_SITE_URL || 'https://solaris.vn';
    const now = new Date();

    const staticRoutes: MetadataRoute.Sitemap = [
        { url: baseUrl, lastModified: now, changeFrequency: 'daily', priority: 1.0 },
        { url: `${baseUrl}/san-pham`, lastModified: now, changeFrequency: 'daily', priority: 0.9 },
        { url: `${baseUrl}/khuyen-mai`, lastModified: now, changeFrequency: 'daily', priority: 0.8 },
        { url: `${baseUrl}/dang-nhap`, lastModified: now, changeFrequency: 'monthly', priority: 0.3 },
        { url: `${baseUrl}/dang-ky`, lastModified: now, changeFrequency: 'monthly', priority: 0.3 },
    ];

    try {
        const [prodRes, categories, promos] = await Promise.all([
            shopProductApi.getAll({ pageSize: 100 }).catch(() => ({ items: [] })),
            shopProductApi.getCategories().catch(() => []),
            shopProductApi.getPromotions().catch(() => []),
        ]);

        const productRoutes: MetadataRoute.Sitemap = (prodRes.items || []).map((p: ShopProductCard) => ({
            url: `${baseUrl}/san-pham/${p.slug}`,
            lastModified: now,
            changeFrequency: 'weekly',
            priority: 0.8,
        }));

        const categoryRoutes: MetadataRoute.Sitemap = categories.flatMap((g: ShopCategoryTree) => [
            { url: `${baseUrl}/danh-muc/${g.groupSlug}`, lastModified: now, changeFrequency: 'weekly', priority: 0.7 },
            ...g.categories.map((c: ShopCategoryItem) => ({
                url: `${baseUrl}/danh-muc/${c.categorySlug}`,
                lastModified: now,
                changeFrequency: 'weekly' as const,
                priority: 0.7,
            }))
        ]);

        const promoRoutes: MetadataRoute.Sitemap = promos.map((pr: ShopPromotionBadge) => ({
            url: `${baseUrl}/khuyen-mai/${pr.slug}`,
            lastModified: now,
            changeFrequency: 'weekly',
            priority: 0.6,
        }));

        return [...staticRoutes, ...categoryRoutes, ...productRoutes, ...promoRoutes];
    } catch {
        return staticRoutes;
    }
}


