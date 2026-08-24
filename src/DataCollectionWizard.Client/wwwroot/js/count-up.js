/**
 * Counts figures up (or down) to their new value instead of swapping them.
 *
 * Shared, because three places want it: the grid's per-cloud header counts, the datapoint meter under the title
 * and the distribution beside the bulk panel's buttons. A bulk change moves all of them at once and by a lot,
 * and a number that simply appears in its new state is easy to miss.
 *
 * The text is written here rather than by Blazor, and that is the whole trick: a re-render would otherwise have
 * replaced the previous value before anything could read it, leaving nothing to count from.
 *
 * Mark up an element with:
 *   data-counts="180,63"          one or more integers, in the order the format expects
 *   data-count-format="{0} on · {1} off"
 */

const DURATION_MS = 420;

export function animateCounts(container) {
    if (!container) {
        return;
    }

    container.querySelectorAll("[data-counts]").forEach(function (element) {
        const targets = element.getAttribute("data-counts")
            .split(",")
            .map(function (part) { return parseInt(part, 10) || 0; });

        const format = element.getAttribute("data-count-format") || "{0}";
        const write = function (values) {
            element.textContent = format.replace(/\{(\d+)\}/g, function (_, index) { return values[index]; });
        };

        const from = element.countsShown;
        const unchanged = from
            && from.length === targets.length
            && from.every(function (value, index) { return value === targets[index]; });

        // First sight of this element, a different shape, or nothing moved: show it and be done.
        if (!from || from.length !== targets.length || unchanged) {
            element.countsShown = targets;
            write(targets);
            return;
        }

        if (element.countFrame) {
            cancelAnimationFrame(element.countFrame);
        }

        const started = performance.now();

        const step = function (now) {
            const progress = Math.min(1, (now - started) / DURATION_MS);
            // Ease out, so it arrives gently rather than stopping dead.
            const eased = 1 - Math.pow(1 - progress, 3);

            write(targets.map(function (target, index) {
                return Math.round(from[index] + (target - from[index]) * eased);
            }));

            if (progress < 1) {
                element.countFrame = requestAnimationFrame(step);
            } else {
                element.countsShown = targets;
                element.countFrame = 0;
            }
        };

        element.countFrame = requestAnimationFrame(step);
    });
}
