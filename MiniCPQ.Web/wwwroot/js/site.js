(() => {
  function initializeServerMaterialLimits() {
    const form = document.querySelector("form .material-option")?.closest("form");
    const message = document.querySelector("#server-material-limit-message");
    if (!form || !message) return;

    const options = Array.from(form.querySelectorAll(".material-option"));

    function validateLimits() {
      const totals = new Map();
      const limits = new Map();
      const invalidSelections = [];

      for (const option of options) {
        const toggle = option.querySelector(".material-toggle");
        const quantityInput = option.querySelector(".material-quantity");
        const group = option.dataset.materialGroup;
        const limit = Number.parseInt(option.dataset.limit, 10);
        quantityInput.disabled = !toggle.checked;

        if (!toggle.checked) continue;
        const quantity = Number.parseInt(quantityInput.value, 10);
        if (!Number.isFinite(quantity) || quantity <= 0) {
          invalidSelections.push("已选材料的数量必须大于零");
          continue;
        }

        totals.set(group, (totals.get(group) ?? 0) + quantity);
        if (Number.isFinite(limit)) limits.set(group, limit);
      }

      const labels = { CPU: "CPU", RAM: "内存", ROM: "硬盘", MOTHERBOARD: "主板" };
      const errors = [...invalidSelections];
      for (const [group, total] of totals) {
        const limit = limits.get(group);
        if (Number.isFinite(limit) && total > limit) {
          errors.push(`${labels[group] ?? group}最多 ${limit} 个，当前选择 ${total} 个`);
        }
      }

      message.textContent = errors.join("；");
      message.classList.toggle("d-none", errors.length === 0);
      return errors.length === 0;
    }

    form.addEventListener("change", validateLimits);
    form.addEventListener("input", validateLimits);
    form.addEventListener("submit", event => {
      if (!validateLimits()) {
        event.preventDefault();
        message.scrollIntoView({ behavior: "smooth", block: "center" });
      }
    });
    validateLimits();
  }

  function initializeApprovalPricing() {
    const form = document.querySelector("#approval-pricing-form");
    if (!form) return;

    const cost = Number.parseFloat(form.dataset.cost);
    const cnyPerUnit = Number.parseFloat(form.dataset.cnyPerUnit ?? "1");
    const foreignCurrencyCode = form.dataset.currencyCode ?? "CNY";
    const marginInput = form.querySelector("#gross-margin-percent");
    const priceInput = form.querySelector("#final-price");
    const profitOutput = form.querySelector("#pricing-profit");
    const foreignPriceOutput = form.querySelector("#pricing-foreign-price");
    const messageOutput = form.querySelector("#pricing-message");
    const approveButton = form.querySelector("#approve-quote-button");
    const cnyCurrency = new Intl.NumberFormat("zh-CN", {
      style: "currency",
      currency: "CNY",
      minimumFractionDigits: 2
    });
    const foreignCurrency = new Intl.NumberFormat("zh-CN", {
      style: "currency",
      currency: foreignCurrencyCode,
      minimumFractionDigits: 2
    });

    const roundMoney = value => Math.round((value + Number.EPSILON) * 100) / 100;

    function refreshSummary(price) {
      const isValid = Number.isFinite(price) && price > 0 && price >= cost;
      approveButton.disabled = !isValid;

      if (!Number.isFinite(price) || price <= 0) {
        profitOutput.textContent = "—";
        if (foreignPriceOutput) foreignPriceOutput.textContent = "—";
        messageOutput.textContent = "请输入有效的最终售价";
        messageOutput.classList.add("pricing-warning");
        return;
      }

      profitOutput.textContent = cnyCurrency.format(price - cost);
      if (foreignPriceOutput) foreignPriceOutput.textContent = foreignCurrency.format(price / cnyPerUnit);
      messageOutput.textContent = price < cost ? "售价不能低于成本" : "售价和毛利率将在批准时冻结";
      messageOutput.classList.toggle("pricing-warning", price < cost);
    }

    function updatePriceFromMargin() {
      const margin = Number.parseFloat(marginInput.value);
      if (!Number.isFinite(margin) || margin < 0 || margin >= 100) {
        priceInput.value = "";
        refreshSummary(Number.NaN);
        messageOutput.textContent = "毛利率必须在 0%（含）到 100%（不含）之间";
        return;
      }

      const price = roundMoney(cost / (1 - margin / 100));
      priceInput.value = price.toFixed(2);
      refreshSummary(price);
    }

    function updateMarginFromPrice() {
      const price = Number.parseFloat(priceInput.value);
      if (!Number.isFinite(price) || price <= 0) {
        marginInput.value = "";
        refreshSummary(price);
        return;
      }

      const margin = (price - cost) / price * 100;
      marginInput.value = margin.toFixed(2);
      refreshSummary(price);
    }

    marginInput.addEventListener("input", updatePriceFromMargin);
    priceInput.addEventListener("input", updateMarginFromPrice);
    form.addEventListener("submit", event => {
      const price = Number.parseFloat(priceInput.value);
      if (!Number.isFinite(price) || price <= 0 || price < cost) {
        event.preventDefault();
        refreshSummary(price);
        priceInput.focus();
        return;
      }

      if (!window.confirm("确定以当前售价批准这张报价吗？")) {
        event.preventDefault();
      }
    });

    updatePriceFromMargin();
  }

  initializeServerMaterialLimits();
  initializeApprovalPricing();
})();
