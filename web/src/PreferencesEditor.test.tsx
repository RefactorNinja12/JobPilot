import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { Preferences } from "./api/profile";
import { PreferencesEditor } from "./PreferencesEditor";
import { respondToPlaceSearch, respondToPreferenceCalls } from "./test/server";

const noLimit: Preferences = {
  acceptsFullTime: true,
  acceptsPartTime: true,
  excludeStaffingAgencies: false,
  excludeSeniorRoles: false,
  maxRequiredYears: null,
  commute: null,
};
const withLimit: Preferences = {
  ...noLimit,
  commute: { homeLatitude: 57.7089, homeLongitude: 11.9746, maxDistanceKm: 40 },
};

const save = () => screen.getByRole("button", { name: "Spara krav" });

function fakeGeolocation(
  getCurrentPosition: Geolocation["getCurrentPosition"],
) {
  Object.defineProperty(navigator, "geolocation", {
    value: { getCurrentPosition },
    configurable: true,
  });
}

afterEach(() => {
  Reflect.deleteProperty(navigator, "geolocation");
});

describe("PreferencesEditor", () => {
  it("saves only when something changed, sending all dealbreakers", async () => {
    const sent = respondToPreferenceCalls();
    const onChanged = vi.fn();
    render(<PreferencesEditor preferences={noLimit} onChanged={onChanged} />);
    expect(save()).toBeDisabled();

    await userEvent.click(screen.getByRole("checkbox", { name: "Deltid" }));
    await userEvent.click(
      screen.getByRole("checkbox", {
        name: "Dölj annonser från bemanningsföretag",
      }),
    );
    await userEvent.click(save());

    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled());
    expect(sent).toEqual([
      {
        acceptsFullTime: true,
        acceptsPartTime: false,
        excludeStaffingAgencies: true,
        excludeSeniorRoles: false,
        maxRequiredYears: null,
        commute: null,
      },
    ]);
  });

  it("sends the senior-roles dealbreaker when it is ticked", async () => {
    const sent = respondToPreferenceCalls();
    render(<PreferencesEditor preferences={noLimit} onChanged={vi.fn()} />);

    await userEvent.click(
      screen.getByRole("checkbox", { name: /Dölj seniora tjänster/ }),
    );
    await userEvent.click(save());

    await vi.waitFor(() => expect(sent).toHaveLength(1));
    expect(sent[0]).toMatchObject({ excludeSeniorRoles: true });
  });

  it("sends the maximum required years when the limit is ticked", async () => {
    const sent = respondToPreferenceCalls();
    render(<PreferencesEditor preferences={noLimit} onChanged={vi.fn()} />);

    await userEvent.click(
      screen.getByRole("checkbox", { name: /fler års erfarenhet än/ }),
    );
    const years = screen.getByLabelText("Högst (år)");
    await userEvent.clear(years);
    await userEvent.type(years, "2");
    await userEvent.click(save());

    await vi.waitFor(() => expect(sent).toHaveLength(1));
    expect(sent[0]).toMatchObject({ maxRequiredYears: 2 });
  });

  it("shows a saved limit and removes it when unticked", async () => {
    const sent = respondToPreferenceCalls();
    render(
      <PreferencesEditor
        preferences={{ ...noLimit, maxRequiredYears: 5 }}
        onChanged={vi.fn()}
      />,
    );

    expect(screen.getByLabelText("Högst (år)")).toHaveValue(5);
    await userEvent.click(
      screen.getByRole("checkbox", { name: /fler års erfarenhet än/ }),
    );
    await userEvent.click(save());

    await vi.waitFor(() => expect(sent).toHaveLength(1));
    expect(sent[0]).toMatchObject({ maxRequiredYears: null });
  });

  it("will not save with neither full-time nor part-time", async () => {
    render(
      <PreferencesEditor
        preferences={{ ...noLimit, acceptsPartTime: false }}
        onChanged={vi.fn()}
      />,
    );

    await userEvent.click(screen.getByRole("checkbox", { name: "Heltid" }));

    expect(screen.getByText(/Välj minst en/)).toBeInTheDocument();
    expect(save()).toBeDisabled();
  });

  it("sets a commute limit with a typed home location", async () => {
    const sent = respondToPreferenceCalls();
    const onChanged = vi.fn();
    render(<PreferencesEditor preferences={noLimit} onChanged={onChanged} />);

    await userEvent.click(
      screen.getByRole("checkbox", { name: "Begränsa avståndet från hemmet" }),
    );
    await userEvent.clear(
      screen.getByRole("spinbutton", { name: "Högst (km fågelvägen)" }),
    );
    await userEvent.type(
      screen.getByRole("spinbutton", { name: "Högst (km fågelvägen)" }),
      "25",
    );
    await userEvent.type(
      screen.getByRole("spinbutton", { name: "Latitud" }),
      "59.33",
    );
    await userEvent.type(
      screen.getByRole("spinbutton", { name: "Longitud" }),
      "18.07",
    );
    await userEvent.click(save());

    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled());
    expect(sent).toEqual([
      {
        ...noLimit,
        commute: {
          homeLatitude: 59.33,
          homeLongitude: 18.07,
          maxDistanceKm: 25,
        },
      },
    ]);
  });

  it("removes the commute limit", async () => {
    const sent = respondToPreferenceCalls();
    render(<PreferencesEditor preferences={withLimit} onChanged={vi.fn()} />);

    await userEvent.click(
      screen.getByRole("checkbox", { name: "Begränsa avståndet från hemmet" }),
    );
    await userEvent.click(save());

    await vi.waitFor(() => expect(sent).toEqual([noLimit]));
  });

  it("fills in the home location from the browser, rounded to about 100 m", async () => {
    fakeGeolocation((success) =>
      success({
        coords: { latitude: 57.708912, longitude: 11.974634 },
      } as GeolocationPosition),
    );
    render(<PreferencesEditor preferences={noLimit} onChanged={vi.fn()} />);

    await userEvent.click(
      screen.getByRole("checkbox", { name: "Begränsa avståndet från hemmet" }),
    );
    await userEvent.click(
      screen.getByRole("button", { name: "Använd min position" }),
    );

    expect(screen.getByRole("spinbutton", { name: "Latitud" })).toHaveValue(
      57.709,
    );
    expect(screen.getByRole("spinbutton", { name: "Longitud" })).toHaveValue(
      11.975,
    );
  });

  it("fills in the home location from a place name", async () => {
    respondToPlaceSearch([
      {
        name: "Kvissleby",
        municipality: "Sundsvall",
        latitude: 62.3123,
        longitude: 17.3456,
      },
    ]);
    render(<PreferencesEditor preferences={noLimit} onChanged={vi.fn()} />);

    await userEvent.click(
      screen.getByRole("checkbox", { name: "Begränsa avståndet från hemmet" }),
    );
    await userEvent.type(
      screen.getByRole("textbox", { name: "Sök ort" }),
      "kviss",
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Kvissleby, Sundsvall" }),
    );

    expect(screen.getByRole("spinbutton", { name: "Latitud" })).toHaveValue(
      62.312,
    );
    expect(screen.getByRole("spinbutton", { name: "Longitud" })).toHaveValue(
      17.346,
    );
    expect(
      screen.queryByRole("list", { name: "Ortsförslag" }),
    ).not.toBeInTheDocument();
  });

  it("says so when the browser will not give the position", async () => {
    fakeGeolocation((_, failure) =>
      failure?.({ code: 1, message: "denied" } as GeolocationPositionError),
    );
    render(<PreferencesEditor preferences={noLimit} onChanged={vi.fn()} />);

    await userEvent.click(
      screen.getByRole("checkbox", { name: "Begränsa avståndet från hemmet" }),
    );
    await userEvent.click(
      screen.getByRole("button", { name: "Använd min position" }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent(
      "Kunde inte hämta din position",
    );
  });

  it("has no position button when the browser cannot locate", async () => {
    render(<PreferencesEditor preferences={noLimit} onChanged={vi.fn()} />);

    await userEvent.click(
      screen.getByRole("checkbox", { name: "Begränsa avståndet från hemmet" }),
    );

    expect(
      screen.queryByRole("button", { name: "Använd min position" }),
    ).not.toBeInTheDocument();
  });

  it("shows the API message when saving fails", async () => {
    respondToPreferenceCalls({
      status: 400,
      body: {
        code: "CommuteLimit.DistanceOutOfRange",
        detail: "Max commute distance must be between 1 and 500 km.",
      },
    });
    const onChanged = vi.fn();
    render(<PreferencesEditor preferences={withLimit} onChanged={onChanged} />);

    await userEvent.click(screen.getByRole("checkbox", { name: "Deltid" }));
    await userEvent.click(save());

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Max commute distance must be between 1 and 500 km.",
    );
    expect(onChanged).not.toHaveBeenCalled();
  });
});
